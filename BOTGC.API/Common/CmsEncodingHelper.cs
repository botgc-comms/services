using System;
using System.Collections.Generic;
using System.Net.Http;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Common
{
    public sealed class CmsEncodingHelper : ICmsEncodingHelper
    {
        public string EncodePageContent(string html)
        {
            if (html == null) throw new ArgumentNullException(nameof(html));
            var normalised = html.Replace("\r\n", "\n").Replace("\r", "\n");
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("v", normalised)
            });
            var pair = content.ReadAsStringAsync().GetAwaiter().GetResult();
            var idx = pair.IndexOf('=');
            return idx >= 0 && idx < pair.Length - 1 ? pair.Substring(idx + 1) : string.Empty;
        }
    }
}
