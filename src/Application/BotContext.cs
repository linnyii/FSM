using Application.Quiz;
using Bot;

namespace Application;

public sealed class BotContext(IMessenger messenger, int initialTokenQuota, IQuizBank? quizBank = null)
    : IBotContext
{
    public int TokenQuota { get; private set; } = initialTokenQuota;
    public IMessenger Messenger { get; } = messenger;
    public User? CurrentUser { get; set; }

    // 可變的使用者表(login 事件建/更新);對外以 IReadOnlyDictionary 曝露。
    private readonly Dictionary<string, User> _users = new();
    public IReadOnlyDictionary<string, User> Users => _users;
    public int OnlineCount { get; set; }
    public bool SomeoneIsBroadcasting { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public int ElapsedSecondsInQuestion { get; set; }
    public int ElapsedSecondsInGame { get; set; }
    public int ElapsedSecondsInThanks { get; set; }
    public string? FirstCorrectAnswerer { get; set; }
    public IQuizBank QuizBank { get; } = quizBank ?? new ChoiceQuizBank();
    public List<string> RecordBuffer { get; } = [];

    /// <summary>錄音者（下 record 指令者）→ Record Replay 的 @標記對象。</summary>
    public string? RecorderId { get; set; }

    public void DeductQuota(int amount) => TokenQuota -= amount;
    public User UpsertUser(string id, bool isAdmin)
    {
        var user = new User(id, isAdmin) { IsOnline = true };
        _users[id] = user;
        return user;
    }

    /// <summary>把當前發話者設為已知使用者(訊息作者);未知則以非 admin 建一個臨時 User。</summary>
    public void SetCurrentUser(string id) =>
        CurrentUser = _users.TryGetValue(id, out var u) ? u : new User(id, isAdmin: false);

    public void ShowInitialQuota(int quota) => TokenQuota = quota;
}
