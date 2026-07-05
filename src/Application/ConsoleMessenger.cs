using Bot;

namespace Application;

/// <summary>
/// 最外層的 Messenger 具體實作——懂這題的 I/O 格式（🤖: 、@ 逗號空格）。
/// 由 main 注入到 context；FSM/bot 層只依賴 <see cref="IMessenger"/> 介面，不知道這些格式。
/// </summary>
public sealed class ConsoleMessenger : IMessenger
{
    private readonly TextWriter _out;

    public ConsoleMessenger(TextWriter? output = null) => _out = output ?? Console.Out;

    public void SendChat(string content, IReadOnlyList<int>? tags = null) =>
        _out.WriteLine($"🤖: {content}{FormatTags(tags)}");

    public void CommentPost(string postId, string content, IReadOnlyList<int>? tags = null) =>
        _out.WriteLine($"🤖 (post {postId}): {content}{FormatTags(tags)}");

    public void GoBroadcasting() => _out.WriteLine("🤖 go broadcasting...");

    public void Speak(string content) => _out.WriteLine($"🤖 speaking: {content}");

    public void StopBroadcasting() => _out.WriteLine("🤖 stop broadcasting");

    private static string FormatTags(IReadOnlyList<int>? tags) =>
        tags is { Count: > 0 } ? " " + string.Join(", ", tags.Select(t => $"@{t}")) : string.Empty;
}
