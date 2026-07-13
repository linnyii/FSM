using System.Text.Json;

namespace Application.Parsing;

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
