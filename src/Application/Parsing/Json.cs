using System.Text.Json;

namespace Application.Parsing;

/// <summary>共用的 JSON 反序列化設定：key 大小寫不敏感（json 用 camelCase，record 用 PascalCase）。</summary>
internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new FormatException($"Cannot deserialize '{json}' to {typeof(T).Name}.");
}
