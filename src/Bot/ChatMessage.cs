namespace Bot;


public sealed class ChatMessage(string authorId, string content, bool tagsBot, IReadOnlyList<string>? tags = null)
{
    public string AuthorId { get; } = authorId;
    public string Content { get; } = content;
    public bool TagsBot { get; } = tagsBot;

    public IReadOnlyList<string> Tags { get; } = tags ?? [];
}
