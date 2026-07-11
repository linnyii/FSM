using Bot;

namespace Application.Output;

/// <summary>聊天室頻道的輸出格式:機器人 <c>🤖: content @tags</c>。</summary>
public sealed class ChatRoomView(TextWriter output)
{
    public void BotSays(string content, IReadOnlyList<string>? tags = null) =>
        output.WriteLine($"🤖: {content}{Tags.Format(tags)}");
}
