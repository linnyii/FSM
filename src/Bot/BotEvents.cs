namespace Bot;

/// <summary>bot module 內建的事件名（Waterball 通用機制層級的事件）。</summary>
public static class BotEvents
{
    /// <summary>聊天訊息 —— 指令的載體（.command() 把 on 固定成這個）。</summary>
    public const string NewMessage = "new message";
}
