// Services/DeepLinkService.cs
using System.Collections.Concurrent;
using BOTGC.Mobile.Interfaces;

namespace BOTGC.Mobile.Services;

public sealed class DeepLinkService : IDeepLinkService
{
    private readonly ConcurrentQueue<Uri> _pending = new();

    public event EventHandler<Uri>? LinkReceived;

    public void Publish(Uri uri)
    {
        _pending.Enqueue(uri);
        LinkReceived?.Invoke(this, uri);
    }

    public bool TryDequeue(out Uri? uri)
    {
        if (_pending.TryDequeue(out var u))
        {
            uri = u;
            return true;
        }

        uri = null;
        return false;
    }
}
