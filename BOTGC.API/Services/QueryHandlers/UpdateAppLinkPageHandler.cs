using System.Text;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using NUglify;

namespace BOTGC.API.Services.QueryHandlers
{
    public sealed class UpdateAppLinkPageHandler(
            IOptions<AppSettings> settings,
            ILogger<UpdateAppLinkPageHandler> logger,
            IMediator mediator,
            IBlobStorageService blobStorage)
        : QueryHandlerBase<UpdateAppLinkPageCommand, bool>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<UpdateAppLinkPageHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IBlobStorageService _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));

        public override async Task<bool> Handle(UpdateAppLinkPageCommand request, CancellationToken cancellationToken)
        {
            var htmlChunk = new StringBuilder();

            htmlChunk.AppendLine(@"<style>
              .app-link-wrap{max-width:720px;margin:0 auto;}
              .app-link-wrap h1{margin-bottom:8px;}
              .app-link-card{background:#ffffff;border:1px solid #e5e5e5;border-radius:10px;padding:16px 18px;margin:18px 0;}
              .app-link-qr{display:block;margin:14px auto 8px auto;width:280px;max-width:100%;height:auto;}
              .app-link-muted{color:#5a5a5a;font-size:14px;margin-top:6px;}
              .app-link-actions{margin-top:12px;}
              #open-on-this-phone{display:none;}
              .app-link-actions a{display:inline-block;background:#1d70b8;color:#fff;text-decoration:none;padding:10px 14px;border-radius:6px;}
              .app-link-actions a:hover{text-decoration:underline;}

              @media (pointer: coarse) and (max-width: 1024px){
                #open-on-this-phone{display:inline-block;}
              }
            </style>");

            htmlChunk.AppendLine(@"<div class=""app-link-wrap"">");
            htmlChunk.AppendLine(@"  <h1>Burton Juniors App</h1>");
            htmlChunk.AppendLine(@"  <p>Use this page to link the Burton Juniors app to your club account.</p>");

            htmlChunk.AppendLine(@"  <div class=""app-link-card"">");
            htmlChunk.AppendLine(@"    <h2 style=""margin-top:0;"">Step 1 — Download the app</h2>");
            htmlChunk.AppendLine(@"    <p>Install the <strong>Burton Juniors</strong> app on your iPhone.</p>");
            htmlChunk.AppendLine(@"  </div>");

            htmlChunk.AppendLine(@"  <div class=""app-link-card"">");
            htmlChunk.AppendLine(@"    <h2 style=""margin-top:0;"">Step 2 — Link your account</h2>");
            htmlChunk.AppendLine(@"    <p><strong>Most people:</strong> open this page on a computer, then use the app to scan the QR code below.</p>");
            htmlChunk.AppendLine(@"    <p><strong>If you are already on your phone:</strong> you can use the link that appears below the QR code to open the app and link directly.</p>");
            htmlChunk.AppendLine(@"    <img id=""probe-image"" class=""app-link-qr"" src=""/images/resources/burtonontrent/loading.gif"" alt=""QR code for linking the Burton Juniors app"" />");
            htmlChunk.AppendLine(@"    <p class=""app-link-muted"">If the QR code does not appear, refresh the page.</p>");
            htmlChunk.AppendLine(@"    <div class=""app-link-actions"">");
            htmlChunk.AppendLine(@"      <a id=""open-on-this-phone"" href=""#"" rel=""nofollow"">Open on this device</a>");
            htmlChunk.AppendLine(@"    </div>");
            htmlChunk.AppendLine(@"  </div>");
            htmlChunk.AppendLine(@"</div>");

            var jsSource = """
                window.addEventListener("load", () => {
                  try {
                    var p = window.properties, u = window.userID;
                    if (typeof u !== "string" || !p || typeof p !== "object") return;

                    var j = JSON.stringify(p);
                    var b = btoa(unescape(encodeURIComponent(j)));
                    var q = encodeURIComponent(b);

                    var img = document.getElementById("probe-image");
                    if (img) img.src = "https://botgc.link/appauth-code?mode=qr&q=" + q;

                    var a = document.getElementById("open-on-this-phone");
                    if (a) {
                      a.href = "https://botgc.link/appauth-code?mode=activate&q=" + q;
                      a.style.display = "inline-block";
                    }
                  } catch (_) { }
                });
                """;

            var minified = Uglify.Js(jsSource);

            if (minified.HasErrors)
            {
                var errorText = string.Join(" | ", minified.Errors.Select(e => e.ToString()));
                throw new InvalidOperationException($"JS minification failed: {errorText}");
            }

            var jsChunk = minified.Code;

            var page = new StringBuilder();

            page.AppendLine(@"<div class=""inner-page-wrapper"">");
            page.AppendLine(@"  <div class=""inner-main"">");
            page.AppendLine(@"    <div class=""page-space"">");
            page.AppendLine(@"      <div class=""inner-nav"">");
            page.AppendLine(@"        <div class=""wysiwyg-editable"">{MENU,section=Hospitality and Hire}</div>");
            page.AppendLine(@"      </div>");
            page.AppendLine(@"      <div class=""inner-full"">");
            page.AppendLine(@"        <div class=""wysiwyg-editable"">");
            page.AppendLine(htmlChunk.ToString().Trim());
            page.AppendLine(@"<script>");
            page.AppendLine(jsChunk);
            page.AppendLine(@"</script>");
            page.AppendLine(@"        </div>");
            page.AppendLine(@"      </div>");
            page.AppendLine(@"    </div>");
            page.AppendLine(@"  </div>");
            page.AppendLine(@"</div>");

            var html = page.ToString();

            var updateRequest = new UpdateCmsPageCommand(_settings.IG.AppLinkPageId, html);
            var result = await _mediator.Send(updateRequest, cancellationToken);

            return result;
        }
    }
}
