// Interfaces/IAppAuthService.cs
namespace BOTGC.Mobile.Interfaces;

public interface INavigationGate
{
    bool IsTaken { get; }
    bool TryTake();
    void Take();
}
