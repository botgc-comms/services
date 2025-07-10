using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace BOTGC.Leaderboards.Services
{
    public class CompetitionHub : Hub
    {
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }
    }

}
