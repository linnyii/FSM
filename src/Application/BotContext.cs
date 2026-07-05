using Bot;

namespace Application;

/// <summary>
/// Waterball 這一隻機器人的具體 context——共享的黑板，塞滿 Waterball 專屬欄位。
/// 實作 <see cref="IBotContext"/> 把「機器人領域通用能力」（額度/權限/messenger）填上具體值，
/// 額外持有 Waterball 專屬的線上人數、廣播狀態、當前發話者等。
/// FSM 核心只看到型別參數 C，不認得這些欄位（設計第 9 節解法二·泛型）。
/// </summary>
public sealed class BotContext : IBotContext
{
    // ── 機器人領域通用（IBotContext）──
    public int TokenQuota { get; private set; }
    public IMessenger Messenger { get; }

    /// <summary>當前正在處理的事件的發話者是不是管理員。</summary>
    public bool IsCurrentUserAdmin { get; set; }

    // ── Waterball 專屬（跨狀態共享一份）──
    public int OnlineCount { get; set; }
    public bool SomeoneIsBroadcasting { get; set; }

    /// <summary>知識王目前答到第幾題（跨子狀態共享）。</summary>
    public int CurrentQuestionIndex { get; set; }

    public BotContext(IMessenger messenger, int initialTokenQuota)
    {
        Messenger = messenger;
        TokenQuota = initialTokenQuota;
    }

    public void DeductQuota(int amount) => TokenQuota -= amount;
}
