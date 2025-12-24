using BOTGC.Mobile.Interfaces;
using BOTGC.Mobile.Pages;

namespace BOTGC.Mobile.Services;

public sealed class DateOfBirthFlowHandler
{
    private readonly IAppAuthService _authService;

    public DateOfBirthFlowHandler(IAppAuthService authService)
    {
        _authService = authService;
    }

    public void Wire(DateOfBirthPage page)
    {
        //page.DateOfBirthConfirmed += async (sessionId, dob) =>
        //{
        //    try
        //    {
        //        var tokens = await _authService.IssueTokenAsync(sessionId, dob);
        //        await _authService.SaveTokensAsync(tokens);

        //        MainThread.BeginInvokeOnMainThread(() =>
        //        {
        //            var services = Application.Current!.Handler!.MauiContext!.Services;
        //            Application.Current!.MainPage = new NavigationPage(services.GetRequiredService<WebShellPage>());

        //        });
        //    }
        //    catch
        //    {
        //        MainThread.BeginInvokeOnMainThread(async () =>
        //        {
        //            await Application.Current!.MainPage!.DisplayAlert("Could not sign in", "Date of birth did not match. Try again.", "OK");
        //        });
        //    }
        //};
    }
}
