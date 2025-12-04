using Microsoft.AspNetCore.SignalR;

namespace BOTGC.MemberPortal.Hubs
{
    public sealed class NgrokHub : Hub
    {
        private readonly NgrokState _state;

        public NgrokHub(NgrokState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public override async Task OnConnectedAsync()
        {
            if (!string.IsNullOrWhiteSpace(_state.PublicUrl))
            {
                await Clients.Caller.SendAsync("NgrokUrlAvailable", _state.PublicUrl);
            }

            await base.OnConnectedAsync();
        }
    }
}
