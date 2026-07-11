using Bot;
using Fsm.Core;

namespace Application.Parsing;

/// <summary><c>[new post] {"id","authorId","title","content","tags":[]}</c> → NewPost。</summary>
public sealed class NewPostParser : IEventParser
{
    public string Name => BotEvents.NewPost;

    private sealed record Dto(string Id, string AuthorId, string Title, string Content, List<string>? Tags);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        var post = new NewPost(dto.Id, dto.AuthorId, dto.Title, dto.Content, dto.Tags ?? []);
        return new Event(BotEvents.NewPost, post);
    }
}
