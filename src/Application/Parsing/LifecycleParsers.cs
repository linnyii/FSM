using Application.Events;
using Bot;

namespace Application.Parsing;

public sealed class LoginParser : IEventParser
{
    public string Name => BotEvents.Login;

    private sealed record Dto(string UserId, bool IsAdmin);

    public IDomainEvent Parse(string contentJson)
    {
        var dto = Json.Deserialize<Dto>(contentJson);
        return new LoginEvent(dto.UserId, dto.IsAdmin);
    }
}

public sealed class LogoutParser : IEventParser
{
    public string Name => BotEvents.Logout;

    private sealed record Dto(string UserId);

    public IDomainEvent Parse(string contentJson) =>
        new LogoutEvent(Json.Deserialize<Dto>(contentJson).UserId);
}

public sealed class StartedParser : IEventParser
{
    public string Name => BotEvents.Started;

    private sealed record Dto(string Time, int Quota);

    public IDomainEvent Parse(string contentJson)
    {
        var dto = Json.Deserialize<Dto>(contentJson);
        return new StartedEvent(dto.Time, dto.Quota);
    }
}

public sealed class EndParser : IEventParser
{
    public string Name => BotEvents.End;

    public IDomainEvent Parse(string contentJson) => new EndEvent();
}
