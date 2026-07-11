using Bot;
using Fsm.Core;

namespace Application.Parsing;

/// <summary><c>[login] {"userId","isAdmin"}</c> → LoginInfo。</summary>
public sealed class LoginParser : IEventParser
{
    public string Name => BotEvents.Login;

    private sealed record Dto(string UserId, bool IsAdmin);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        return new Event(BotEvents.Login, new LoginInfo(dto.UserId, dto.IsAdmin));
    }
}

/// <summary><c>[logout] {"userId"}</c> → LogoutInfo。</summary>
public sealed class LogoutParser : IEventParser
{
    public string Name => BotEvents.Logout;

    private sealed record Dto(string UserId);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        return new Event(BotEvents.Logout, new LogoutInfo(dto.UserId));
    }
}

/// <summary><c>[started] {"time","quota"}</c> → StartedInfo（帶初始 quota）。</summary>
public sealed class StartedParser : IEventParser
{
    public string Name => BotEvents.Started;

    private sealed record Dto(string Time, int Quota);

    public Event Parse(string json)
    {
        var dto = Json.Deserialize<Dto>(json);
        return new Event(BotEvents.Started, new StartedInfo(dto.Time, dto.Quota));
    }
}

/// <summary><c>[end]</c> → 無 payload。</summary>
public sealed class EndParser : IEventParser
{
    public string Name => BotEvents.End;

    public Event Parse(string json) => new(BotEvents.End);
}
