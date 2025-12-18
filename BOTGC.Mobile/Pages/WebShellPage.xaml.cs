using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace BOTGC.Mobile;

public partial class WebShellPage : ContentPage
{
    private readonly Dictionary<string, string> _routes = new()
    {
        ["home"] = "https://www.botgc.co.uk/",
        ["progress"] = "https://www.botgc.co.uk/membership",
        ["vouchers"] = "https://www.botgc.co.uk/visitor_pages",
        ["mentor"] = "https://www.botgc.co.uk/the_course",
        ["play"] = "https://www.botgc.co.uk/hospitality_and_hire"
    };

    public WebShellPage()
    {
        InitializeComponent();

        BottomNav.TabTapped += Navigate;

        Navigate("home");
    }

    private void Navigate(string key)
    {
        if (!_routes.TryGetValue(key, out var url))
        {
            return;
        }

        //BottomNav.SelectedKey = key;

        Browser.Source = new UrlWebViewSource
        {
            Url = url
        };
    }
}
