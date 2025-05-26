using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BOTGC.MembershipApplication
{
    public class ContentSecurityPolicyBuilder
    {
        private readonly IApplicationBuilder _app;
        private readonly List<string> _scriptSrc = new()
        {
            "'self'",
            "https://cdn.jsdelivr.net",
            "https://www.google.com",
            "https://www.gstatic.com",
            "https://cdn.getaddress.io"
        };

        private readonly List<string> _styleSrc = new()
        {
            "'self'",
            "'unsafe-inline'"
        };

        private readonly List<string> _connectSrc = new()
        {
            "'self'",
            "https://www.google.com",
            "https://www.gstatic.com",
            "https://api.getaddress.io"
        };

        private readonly List<string> _fontSrc = new()
        {
            "'self'",
            "data:"
        };

        private readonly List<string> _frameSrc = new()
        {
            "'self'",
            "https://www.google.com",
            "https://localhost:5001"
        };

        private readonly List<string> _excludedPaths = new();

        private string? _apiUrl;

        public ContentSecurityPolicyBuilder(IApplicationBuilder app)
        {
            _app = app;
        }

        public ContentSecurityPolicyBuilder WithGrowSurf()
        {
            _scriptSrc.Add("https://app.growsurf.com");
            _styleSrc.Add("https://use.typekit.net");
            _styleSrc.Add("https://p.typekit.net");
            _connectSrc.Add("https://api.growsurf.com");
            _fontSrc.Add("https://use.typekit.net");
            _fontSrc.Add("https://p.typekit.net");
            return this;
        }

        public ContentSecurityPolicyBuilder ExcludePaths(params string[] paths)
        {
            foreach (var path in paths)
            {
                _excludedPaths.Add(path.ToLowerInvariant());
            }
            return this;
        }

        public void Build()
        {
            _app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value?.ToLowerInvariant();
                if (_excludedPaths.Contains(path))
                {
                    await next();
                    return;
                }

                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var settings = context.RequestServices.GetRequiredService<AppSettings>();
                _apiUrl = settings.API.Url;

                if (!_connectSrc.Contains(_apiUrl))
                {
                    _connectSrc.Add(_apiUrl);
                }

                if (env.IsDevelopment())
                {
                    if (!_scriptSrc.Contains("'unsafe-inline'"))
                        _scriptSrc.Add("'unsafe-inline'");

                    _connectSrc.Add("http://localhost:*");
                    _connectSrc.Add("https://localhost:*");
                    _connectSrc.Add("ws://localhost:*");
                    _connectSrc.Add("wss://localhost:*");
                }

                var nonceBytes = RandomNumberGenerator.GetBytes(16);
                var nonce = Convert.ToBase64String(nonceBytes);

                var scriptSrcWithNonce = _scriptSrc.Distinct().Append($"'nonce-{nonce}'");

                var csp = string.Join(" ", new[]
                {
                    "default-src 'self';",
                    $"script-src {string.Join(" ", scriptSrcWithNonce)};",
                    $"style-src {string.Join(" ", _styleSrc.Distinct())};",
                    "img-src 'self' data: https://growsurf-blog.s3-us-west-2.amazonaws.com;",
                    $"font-src {string.Join(" ", _fontSrc.Distinct())};",
                    $"connect-src {string.Join(" ", _connectSrc.Distinct())};",
                    $"frame-src {string.Join(" ", _frameSrc.Distinct())};",
                    $"form-action 'self' {_apiUrl};",
                    "base-uri 'self';",
                    "object-src 'none';"
                });

                context.Response.Headers["Content-Security-Policy"] = csp;

                context.Items["CSPNonce"] = nonce;

                await next();
            });
        }
    }
}
