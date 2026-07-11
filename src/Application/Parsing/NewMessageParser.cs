using Bot;
using Fsm.Core;

namespace Application.Parsing;

/// <summary><c>[new message] {"authorId","content","tags":[]}</c> → ChatMessage(TagsBot = tags 含 "bot")。</summary>
public sealed class NewMessageParser : IEventParser
{
    public string Name => BotEvents.NewMessage;

    private sealed record Dto(string AuthorId, string Content, List<string>? Tags);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        var tagsBot = dto.Tags?.Contains("bot") ?? false;
        return new Event(BotEvents.NewMessage, new ChatMessage(dto.AuthorId, dto.Content, tagsBot));
    }
}
