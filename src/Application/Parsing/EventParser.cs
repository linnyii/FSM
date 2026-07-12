using Application.Events;

namespace Application.Parsing;

public sealed class EventParser
{
    private readonly Dictionary<string, IEventParser> _parsers;

    public EventParser(IEnumerable<IEventParser> parsers) =>
        _parsers = parsers.ToDictionary(p => p.Name);

    public IDomainEvent? Parse(string line)
    {
        line = line.Trim();
        if (line.Length == 0)
            return null;

        var (eventId, eventContentJson) = RecognizeEvent(line);

        if (!eventId.EndsWith(" elapsed", StringComparison.Ordinal) && eventId != "elapsed")
            return Resolve(eventId).Parse(eventContentJson);
        var inner = eventId[..^"elapsed".Length].Trim();
        return Resolve("elapsed").Parse(inner);

    }

    private IEventParser Resolve(string name) =>
        _parsers.TryGetValue(name, out var p) ? p : throw new UnknownEventException(name);

    private static (string Shell, string Json) RecognizeEvent(string line)
    {
        var close = line.IndexOf(']');
        if (!line.StartsWith('[') || close < 0)
            throw new FormatException($"Malformed event line: '{line}'. Expected '[name] json'.");

        var shell = line[1..close].Trim();
        var json = line[(close + 1)..].Trim();
        return (shell, json);
    }
}
