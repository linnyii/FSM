namespace Bot;

/// <summary>bot module 內建的事件名（Waterball 通用機制層級的事件）。</summary>
public static class BotEvents
{
    /// <summary>聊天訊息 —— 指令的載體（.command() 把 on 固定成這個）。</summary>
    public const string NewMessage = "new message";

    // 正式輸入格式的事件名（[name] {json}）。
    public const string NewPost = "new post";
    public const string GoBroadcasting = "go broadcasting";
    public const string Speak = "speak";
    public const string StopBroadcasting = "stop broadcasting";
    public const string Login = "login";
    public const string Logout = "logout";
    public const string Elapsed = "elapsed";
    public const string Started = "started";
    public const string End = "end";
}
