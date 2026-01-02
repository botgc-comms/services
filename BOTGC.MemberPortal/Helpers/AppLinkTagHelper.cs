using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BOTGC.MemberPortal.TagHelpers;

[HtmlTargetElement("app-link")]
public sealed class AppLinkTagHelper : TagHelper
{
    [HtmlAttributeName("href")]
    public string? Href { get; set; }

    [HtmlAttributeName("target")]
    public string? Target { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;

        output.Attributes.SetAttribute("data-app-link", "");
        output.Attributes.SetAttribute("role", "link");

        if (!output.Attributes.ContainsName("tabindex"))
        {
            output.Attributes.SetAttribute("tabindex", "0");
        }

        if (!string.IsNullOrWhiteSpace(Href))
        {
            output.Attributes.SetAttribute("data-app-href", Href);
        }

        if (!string.IsNullOrWhiteSpace(Target))
        {
            output.Attributes.SetAttribute("data-app-target", Target);
        }

        output.Attributes.RemoveAll("href");
        output.Attributes.RemoveAll("target");
    }
}
