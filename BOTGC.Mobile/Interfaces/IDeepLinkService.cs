// Interfaces/IAppAuthService.cs
namespace BOTGC.Mobile.Interfaces;

public interface IDeepLinkService
{
    event EventHandler<Uri> LinkReceived;
    void Publish(Uri uri);
}
