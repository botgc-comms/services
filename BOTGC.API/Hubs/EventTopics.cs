namespace BOTGC.API.Services.Realtime;

public static class EventTopics
{
    public static string MemberEvent(int memberId, string eventTypeName)
        => $"m:{memberId}:e:{eventTypeName}";
}
