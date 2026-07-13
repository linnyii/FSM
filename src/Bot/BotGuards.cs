using Fsm.Core;

namespace Bot;

public sealed class CommandIsGuard<C>(string keyword) : IGuard<C>
{
    public bool Test(Event @event, C ctx) =>
        @event.Payload is ChatMessage m && m.TagsBot && m.Content == keyword;
}

public sealed class IsAdminGuard<C> : IGuard<C> where C : IBotContext
{
    public bool Test(Event @event, C ctx) => ctx.CurrentUser?.IsAdmin == true;
}

public sealed class HasQuotaGuard<C>(int amount) : IGuard<C> where C : IBotContext
{
    public bool Test(Event @event, C ctx) => ctx.TokenQuota >= amount;
}
