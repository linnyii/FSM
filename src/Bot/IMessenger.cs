namespace Bot;

/// <summary>
/// 機器人的輸出出口（介面在 bot 層，實作在 application/main 注入 —— 依賴反轉）。
/// 所有要「發出東西」的 Action 都呼叫這同一個介面，不直接 println；
/// 輸出格式（🤖: 、逗號空格、[Record Replay]）是 I/O 細節，屬最外層的 ConsoleMessenger。
/// </summary>
public interface IMessenger
{
    /// <summary>🤖: good to hear @3, @4</summary>
    void SendChat(string content, IReadOnlyList<int>? tags = null);

    /// <summary>在貼文底下留言。</summary>
    void CommentPost(string postId, string content, IReadOnlyList<int>? tags = null);

    /// <summary>🤖 go broadcasting...</summary>
    void GoBroadcasting();

    /// <summary>🤖 speaking: ...</summary>
    void Speak(string content);

    /// <summary>停止廣播。</summary>
    void StopBroadcasting();
}
