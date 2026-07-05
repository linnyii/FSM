namespace Bot;

/// <summary>
/// "new message" 事件的 payload 形狀。bot module 內建「指令一定是聊天訊息、一定 tag bot」
/// 這兩條 Waterball 鐵律，故需認得訊息的作者、內容、以及有沒有 tag 到 bot。
/// </summary>
public sealed class ChatMessage
{
    public int AuthorId { get; }
    public string Content { get; }
    public bool TagsBot { get; }

    public ChatMessage(int authorId, string content, bool tagsBot)
    {
        AuthorId = authorId;
        Content = content;
        TagsBot = tagsBot;
    }
}
