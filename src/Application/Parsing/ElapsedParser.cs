using Application.Events;
using Bot;

namespace Application.Parsing;

public sealed class ElapsedParser : IEventParser
{
    public string Name => BotEvents.Elapsed;

    public IDomainEvent Parse(string contentJson)
    {
        var content = contentJson.Trim();
        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var n))
            throw new FormatException($"Invalid elapsed format: '{contentJson}'. Expected '<n> <unit>'.");

        var seconds = parts[1].ToLowerInvariant() switch
        {
            "seconds" => n,
            "minutes" => n * 60,
            "hours" => n * 3600,
            _ => throw new FormatException($"Unknown time unit: '{parts[1]}'."),
        };
        return new ElapsedEvent(seconds, content);
    }
}
