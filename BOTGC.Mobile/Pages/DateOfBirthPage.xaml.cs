using BOTGC.Mobile.Interfaces;

namespace BOTGC.Mobile.Pages;

public partial class DateOfBirthPage : ContentPage
{
    private readonly IAppAuthService _authService;
    private string? _sessionId;
    private bool _busy;

    public DateOfBirthPage(IAppAuthService authService)
    {
        _authService = authService;

        InitializeComponent();

        DobPicker.MaximumDate = DateTime.Today;
        DobPicker.Date = DateTime.Today.AddYears(-18);

        ContinueButton.Clicked += ContinueButton_Clicked;
    }

    public void SetSessionId(string sessionId)
    {
        _sessionId = sessionId;
    }

    private async void ContinueButton_Clicked(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_sessionId))
        {
            await DisplayAlert("Something went wrong", "No sign-in session was provided. Please try again.", "OK");
            return;
        }

        _busy = true;
        ContinueButton.IsEnabled = false;

        try
        {
            var dob = new DateOnly(DobPicker.Date.Year, DobPicker.Date.Month, DobPicker.Date.Day);

            var tokens = await _authService.IssueTokenAsync(_sessionId, dob);
            await _authService.SaveTokensAsync(tokens);

            var services = Application.Current!.Handler!.MauiContext!.Services;
            Application.Current!.MainPage = new NavigationPage(services.GetRequiredService<WebShellPage>());
        }
        catch
        {
            await DisplayAlert("Could not sign in", "Date of birth did not match. Try again.", "OK");
        }
        finally
        {
            _busy = false;
            ContinueButton.IsEnabled = true;
        }
    }
}
