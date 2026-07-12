using Application.Events;
using Bot;

namespace Application.Parsing;

public sealed class NewMessageParser : IEventParser
{
    public string Name => BotEvents.NewMessage;

    private sealed record Dto(string AuthorId, string Content, List<string>? Tags);

    public IDomainEvent Parse(string contentJson)
    {
        var dto = Json.Deserialize<Dto>(contentJson);
        return new ChatEvent(dto.AuthorId, dto.Content, dto.Tags ?? []);
    }
}
