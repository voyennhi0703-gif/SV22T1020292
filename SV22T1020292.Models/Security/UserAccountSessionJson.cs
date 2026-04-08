using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SV22T1020292.Models.Security;

/// <summary>
/// Chuyển đổi UserAccount ↔ chuỗi JSON để lưu/đọc trong Session.
/// Dùng chung cho cả Admin và Shop.
/// </summary>
public static class UserAccountSessionJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static string Serialize(UserAccount user) =>
        JsonSerializer.Serialize(user, Options);

    public static UserAccount? Deserialize(string? json) =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<UserAccount>(json, Options);
}
