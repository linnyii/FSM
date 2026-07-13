using Bot;

namespace Application.Output;

public sealed class ForumView(TextWriter output)
{
    public void BotComments(string postId, string content, IReadOnlyList<string>? tags = null) =>
        output.WriteLine($"🤖 comment in post {postId}: {content}{Tags.Format(tags)}");
}
