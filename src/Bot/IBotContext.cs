namespace Bot;

public interface IBotContext
{
    int TokenQuota { get; }

    User? CurrentUser { get; }

    IReadOnlyDictionary<string, User> Users { get; }

    IMessenger Messenger { get; }

    void DeductQuota(int amount);
}
