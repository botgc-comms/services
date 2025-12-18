using BOTGC.Mobile.Pages;

namespace BOTGC.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new MetallicSplashPage());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);

#if WINDOWS
        window.Width = 390;
        window.Height = 844;
        window.X = 100;
        window.Y = 100;
#endif

        return window;
    }
}
