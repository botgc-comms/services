// App.xaml.cs
using BOTGC.Mobile.Pages;
using BOTGC.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BOTGC.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services, AppNavigationCoordinator coordinator)
    {
        _services = services;

        InitializeComponent();

        MainPage = new NavigationPage(_services.GetRequiredService<MetallicSplashPage>());

        _ = coordinator;
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
