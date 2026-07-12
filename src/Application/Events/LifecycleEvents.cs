using Bot;
using Fsm.Core;

namespace Application.Events;

/// <summary>login:建/更新使用者表(admin 由 payload 帶),設當前發話者。不回顯(Echo 用預設空實作)。</summary>
public sealed record LoginEvent(string UserId, bool IsAdmin) : IDomainEvent
{
    public void ApplyTo(BotContext ctx)
    {
        ctx.UpsertUser(UserId, IsAdmin);
        ctx.CurrentUser = ctx.Users[UserId];
    }

    public Event ToFsmEvent() => new(BotEvents.Login);
}

public sealed record LogoutEvent(string UserId) : IDomainEvent
{
    public Event ToFsmEvent() => new(BotEvents.Logout);
}

public sealed record StartedEvent(string Time, int Quota) : IDomainEvent
{
    public void ApplyTo(BotContext ctx) => ctx.ShowInitialQuota(Quota);
    public Event ToFsmEvent() => new(BotEvents.Started);
}

public sealed record ElapsedEvent(int Seconds, string Shell) : IDomainEvent
{
    public void Echo(TextWriter output) => output.WriteLine($"🕑 {Shell} elapsed...");
    public Event ToFsmEvent() => new(BotEvents.Elapsed, Seconds);
}

public sealed record EndEvent : IDomainEvent
{
    public Event ToFsmEvent() => new(BotEvents.End);
}
