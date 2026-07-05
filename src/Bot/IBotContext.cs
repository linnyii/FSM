namespace Bot;

/// <summary>
/// bot module 認得的「機器人領域通用」context 能力——額度、權限、輸出出口。
/// bot module 懂「機器人這種東西」的通用機制（額度/權限/輪播），
/// 但不懂「某隻機器人的具體值」（king 額度 5 那種）——那些是 application 的事。
/// application 的具體 <c>BotContext</c> 實作此介面，把具體欄位填進來。
/// </summary>
public interface IBotContext
{
    /// <summary>共享的社群額度（全社群共用一份）。</summary>
    int Quota { get; }

    /// <summary>發指令的人是不是管理員（權限判斷，供 .adminOnly() 用）。</summary>
    bool IsCurrentUserAdmin { get; }

    /// <summary>注入的輸出出口。</summary>
    IMessenger Messenger { get; }

    /// <summary>扣額度（.costs(n) 的 action 階段呼叫；額度共用一份）。</summary>
    void DeductQuota(int amount);
}
