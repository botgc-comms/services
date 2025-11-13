using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class UpdateCmsPageHandler(
    IOptions<AppSettings> settings,
    ILogger<UpdateCmsPageHandler> logger,
    IDataProvider dataProvider
) : QueryHandlerBase<UpdateCmsPageCommand, bool>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<UpdateCmsPageHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

    public async override Task<bool> Handle(UpdateCmsPageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating CMS page {PageId}.", request.PageId);

            var baseUrl = _settings.IG.BaseUrl?.TrimEnd('/') ?? throw new InvalidOperationException("IG.BaseUrl is not configured.");
            var url = $"{baseUrl}/ckeditor.php?pageid={request.PageId}&requestType=ajax&ajaxaction=savepageandexit";

            var form = new Dictionary<string, string>
            {
                ["pagecontent[text]"] = request.Html
            };

            await _dataProvider.PostData(url, form);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update CMS page {PageId}.", request.PageId);
            return false;
        }
    }
}
