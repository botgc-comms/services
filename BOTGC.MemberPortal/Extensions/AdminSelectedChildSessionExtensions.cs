using System.Text.Json;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Http;

namespace BOTGC.MemberPortal.Extensions;

public static class AdminSelectedChildSessionExtensions
{
    private const string Key = "admin:selected-child:v1";

    public static MemberSearchResult? GetSelectedChild(this ISession session, JsonSerializerOptions json)
    {
        var raw = session.GetString(Key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MemberSearchResult>(raw, json);
        }
        catch
        {
            return null;
        }
    }

    public static void SetSelectedChild(this ISession session, MemberSearchResult child, JsonSerializerOptions json)
    {
        var raw = JsonSerializer.Serialize(child, json);
        session.SetString(Key, raw);
    }

    public static void ClearSelectedChild(this ISession session)
    {
        session.Remove(Key);
    }
}
