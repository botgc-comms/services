using MediatR;
using System.Net.Mail;

namespace BOTGC.API.Services.Queries
{
    public sealed record SendMemberEmailCommand(int SenderId) : QueryBase<bool>
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
    }
}
