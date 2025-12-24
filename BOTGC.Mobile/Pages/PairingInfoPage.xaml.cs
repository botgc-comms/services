using BOTGC.Mobile.Pages;

namespace BOTGC.Mobile.Pages;

public partial class PairingInfoPage : ContentPage
{
    public PairingInfoPage()
    {
        InitializeComponent();
    }

    private async void OpenWebsiteButton_Clicked(object? sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync(new Uri("https://botgc.co.uk/login"));
    }

    private async void ContinueToScannerButton_Clicked(object? sender, EventArgs e)
    {
        var services = Application.Current!.Handler!.MauiContext!.Services;
        await Navigation.PushAsync(services.GetRequiredService<AppAuthPage>());
    }
}
