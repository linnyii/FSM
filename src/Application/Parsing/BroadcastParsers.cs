using Application.Events;
using Bot;

namespace Application.Parsing;

public sealed class GoBroadcastingParser : IEventParser
{
    public string Name => BotEvents.GoBroadcasting;

    private sealed record Dto(string SpeakerId);

    public IDomainEvent Parse(string contentJson) =>
        new GoBroadcastingEvent(Json.Deserialize<Dto>(contentJson).SpeakerId);
}

public sealed class SpeakParser : IEventParser
{
    public string Name => BotEvents.Speak;

    private sealed record Dto(string SpeakerId, string Content);

    public IDomainEvent Parse(string contentJson)
    {
        var dto = Json.Deserialize<Dto>(contentJson);
        return new SpeakEvent(dto.SpeakerId, dto.Content);
    }
}

public sealed class StopBroadcastingParser : IEventParser
{
    public string Name => BotEvents.StopBroadcasting;

    private sealed record Dto(string SpeakerId);

    public IDomainEvent Parse(string contentJson) =>
        new StopBroadcastingEvent(Json.Deserialize<Dto>(contentJson).SpeakerId);
}
