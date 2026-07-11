using Bot;

namespace Waterball.Tests;

/// <summary>把發出的訊息記進 list 供事後斷言（設計輸出決策 A：用 spy 測試即可）。</summary>
public sealed class SpyMessenger : IMessenger
{
    public List<string> Log { get; } = new();

    public void SendChat(string content, IReadOnlyList<string>? tags = null) => Log.Add($"chat:{content}");
    public void CommentPost(string postId, string content, IReadOnlyList<string>? tags = null) => Log.Add($"post:{postId}:{content}");
    public void GoBroadcasting() => Log.Add("go-broadcasting");
    public void Speak(string content) => Log.Add($"speak:{content}");
    public void StopBroadcasting() => Log.Add("stop-broadcasting");
}
