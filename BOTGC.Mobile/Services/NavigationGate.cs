using BOTGC.Mobile.Interfaces;

namespace BOTGC.Mobile.Services;

public sealed class NavigationGate : INavigationGate
{
    private int _taken;

    public bool IsTaken => Volatile.Read(ref _taken) == 1;

    public bool TryTake() => Interlocked.CompareExchange(ref _taken, 1, 0) == 0;

    public void Take() => Interlocked.Exchange(ref _taken, 1);
}