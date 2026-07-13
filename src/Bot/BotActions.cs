using Fsm.Core;

namespace Bot;

public sealed class DeductQuotaAction<C>(int amount) : IAction<C> where C : IBotContext
{
    public void Execute(Event @event, C ctx) => ctx.DeductQuota(amount);
}

public sealed class SendChatAction<C>(string content) : IAction<C> where C : IBotContext
{
    public void Execute(Event @event, C ctx) => ctx.Messenger.SendChat(content);
}
