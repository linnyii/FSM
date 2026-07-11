namespace Bot;

/// <summary>
/// "new message" 事件的 payload 形狀。bot module 內建「指令一定是聊天訊息、一定 tag bot」
/// 這兩條 Waterball 鐵律，故需認得訊息的作者、內容、以及有沒有 tag 到 bot。
/// </summary>
public sealed class ChatMessage
{
    public string AuthorId { get; }
    public string Content { get; }
    public bool TagsBot { get; }

    /// <summary>原始 tag 清單(含 "bot")。供輸入回顯 <c>💬 &lt;id&gt;: ... @1, @2</c> 用。</summary>
    public IReadOnlyList<string> Tags { get; }

    public ChatMessage(string authorId, string content, bool tagsBot, IReadOnlyList<string>? tags = null)
    {
        AuthorId = authorId;
        Content = content;
        TagsBot = tagsBot;
        Tags = tags ?? [];
    }
}
