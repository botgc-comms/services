using BOTGC.API.Interfaces;

namespace BOTGC.API.Common;

public sealed class CmsEncodingHelper : ICmsEncodingHelper
{
    public string EncodePageContent(string html)
    {
        if (html == null) throw new ArgumentNullException(nameof(html));
        var normalised = html.Replace("\r\n", "\n").Replace("\n", "\r\n");
        using var content = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("pagecontent[text]", normalised)
            });
        return content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}