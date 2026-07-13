using Bot;

namespace Application.Output;

public sealed class ChatRoomView(TextWriter output)
{
    public void BotSays(string content, IReadOnlyList<string>? tags = null) =>
        output.WriteLine($"🤖: {content}{Tags.Format(tags)}");
}
