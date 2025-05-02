using System.Text.Json.Serialization;

namespace BOTGC.MembershipApplication.Models
{
    public class ParticipantModel
    {
        [JsonPropertyName("participantId")]
        public string ParticipantId { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("referredBy")]
        public string? ReferredBy { get; set; }

        [JsonPropertyName("metadata")]
        public object? Metadata { get; set; }

    }
}
