using Bot;
using Fsm.Core;

namespace Application.Parsing;

/// <summary><c>[go broadcasting] {"speakerId"}</c> → BroadcastInfo。</summary>
public sealed class GoBroadcastingParser : IEventParser
{
    public string Name => BotEvents.GoBroadcasting;

    private sealed record Dto(string SpeakerId);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        return new Event(BotEvents.GoBroadcasting, new BroadcastInfo(dto.SpeakerId));
    }
}

/// <summary><c>[speak] {"speakerId","content"}</c> → SpeakInfo。</summary>
public sealed class SpeakParser : IEventParser
{
    public string Name => BotEvents.Speak;

    private sealed record Dto(string SpeakerId, string Content);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        return new Event(BotEvents.Speak, new SpeakInfo(dto.SpeakerId, dto.Content));
    }
}

/// <summary><c>[stop broadcasting] {"speakerId"}</c> → BroadcastInfo。</summary>
public sealed class StopBroadcastingParser : IEventParser
{
    public string Name => BotEvents.StopBroadcasting;

    private sealed record Dto(string SpeakerId);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        return new Event(BotEvents.StopBroadcasting, new BroadcastInfo(dto.SpeakerId));
    }
}
