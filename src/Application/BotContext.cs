using Application.Quiz;
using Bot;

namespace Application;

public sealed class BotContext(IMessenger messenger, int initialTokenQuota, IQuizBank? quizBank = null)
    : IBotContext
{
    public int TokenQuota { get; private set; } = initialTokenQuota;
    public IMessenger Messenger { get; } = messenger;
    public User? CurrentUser { get; set; }

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

    public string? RecorderId { get; set; }

    public void DeductQuota(int amount) => TokenQuota -= amount;
    public User UpsertUser(string id, bool isAdmin)
    {
        var user = new User(id, isAdmin) { IsOnline = true };
        _users[id] = user;
        return user;
    }

    public void SetCurrentUser(string id) =>
        CurrentUser = _users.TryGetValue(id, out var user) ? user : new User(id, isAdmin: false);

    public void GetInitialQuota(int quota) => TokenQuota = quota;
}
