using Application.Quiz;
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

    /// <summary>當前正在處理的事件的發話者（null = 匿名/系統事件）。</summary>
    public User? CurrentUser { get; set; }

    // 可變的使用者表（login 事件建/更新）;對外以 IReadOnlyDictionary 曝露。
    private readonly Dictionary<string, User> _users = new();
    public IReadOnlyDictionary<string, User> Users => _users;

    // ── Waterball 專屬（跨狀態共享一份）──
    public int OnlineCount { get; set; }
    public bool SomeoneIsBroadcasting { get; set; }

    /// <summary>知識王目前答到第幾題（跨子狀態共享）。</summary>
    public int CurrentQuestionIndex { get; set; }

    // ── timeout 累計（elapsed 事件累加;供批四 guard 分流 20s/1h）──
    /// <summary>本題累計秒（答對/進新題歸零）。</summary>
    public int ElapsedSecondsInQuestion { get; set; }

    /// <summary>全場累計秒（進 KnowledgeKing 歸零;達 3600 強制結束）。</summary>
    public int ElapsedSecondsInGame { get; set; }

    /// <summary>ThanksForJoining 累計秒（進場歸零;達 20 回 Normal）。</summary>
    public int ElapsedSecondsInThanks { get; set; }

    // ── 計分（首答旗標兼 @標記對象）──
    /// <summary>本題首位答對者 id;null = 本題尚無人答對。</summary>
    public string? FirstCorrectAnswerer { get; set; }

    /// <summary>知識王題庫（發題 / 判對錯 / 題數）。</summary>
    public IQuizBank QuizBank { get; }

    // ── 錄音（Record）──
    /// <summary>累積每筆 speak 文字;stop broadcasting 輸出 Record Replay 後 Clear。</summary>
    public List<string> RecordBuffer { get; } = new();

    /// <summary>錄音者（下 record 指令者）→ Record Replay 的 @標記對象。</summary>
    public string? RecorderId { get; set; }

    public BotContext(IMessenger messenger, int initialTokenQuota, IQuizBank? quizBank = null)
    {
        Messenger = messenger;
        TokenQuota = initialTokenQuota;
        QuizBank = quizBank ?? new ChoiceQuizBank();
    }

    public void DeductQuota(int amount) => TokenQuota -= amount;

    /// <summary>login 事件:建/更新使用者表(admin 身份由 login payload 帶)。</summary>
    public User UpsertUser(string id, bool isAdmin)
    {
        var user = new User(id, isAdmin) { IsOnline = true };
        _users[id] = user;
        return user;
    }

    /// <summary>把當前發話者設為已知使用者(訊息作者);未知則以非 admin 建一個臨時 User。</summary>
    public void SetCurrentUser(string id) =>
        CurrentUser = _users.TryGetValue(id, out var u) ? u : new User(id, isAdmin: false);

    /// <summary>started 事件:設初始社群額度。</summary>
    public void SeedQuota(int quota) => TokenQuota = quota;
}
