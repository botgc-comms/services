using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BOTGC.API.Hubs
{
    [Authorize]
    public sealed class EventsHub : Hub
    {
    }
}
