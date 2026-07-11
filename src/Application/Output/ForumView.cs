using Bot;

namespace Application.Output;

/// <summary>論壇頻道的輸出格式:機器人留言 <c>🤖 comment in post &lt;post id&gt;: content @tags</c>。</summary>
public sealed class ForumView(TextWriter output)
{
    public void BotComments(string postId, string content, IReadOnlyList<string>? tags = null) =>
        output.WriteLine($"🤖 comment in post {postId}: {content}{Tags.Format(tags)}");
}
