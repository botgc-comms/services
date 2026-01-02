// Pages/PairingInfoPage.xaml.cs
using BOTGC.Mobile.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace BOTGC.Mobile.Pages;

public partial class PairingInfoPage : ContentPage
{
    public PairingInfoPage()
    {
        InitializeComponent();
    }

    private async void OpenWebsiteButton_Clicked(object? sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync(new Uri("https://www.botgc.co.uk/the_botgc_app"));
    }

    private async void ContinueButton_Clicked(object? sender, EventArgs e)
    {
        var services = Application.Current!.Handler!.MauiContext!.Services;
        await Navigation.PushAsync(services.GetRequiredService<AppAuthPage>());
    }
}
