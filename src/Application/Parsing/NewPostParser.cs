using Application.Events;
using Bot;

namespace Application.Parsing;

public sealed class NewPostParser : IEventParser
{
    public string Name => BotEvents.NewPost;

    private sealed record Dto(string Id, string AuthorId, string Title, string Content, List<string>? Tags);

    public IDomainEvent Parse(string contentJson)
    {
        var dto = Json.Deserialize<Dto>(contentJson);
        return new PostEvent(dto.Id, dto.AuthorId, dto.Title, dto.Content, dto.Tags ?? []);
    }
}
