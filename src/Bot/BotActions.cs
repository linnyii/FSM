using Fsm.Core;

namespace Bot;

/// <summary>額度橫切(扣除):扣掉 n。與 <c>HasQuotaGuard(n)</c> 由 .costs(n) 配對成原子操作。</summary>
public sealed class DeductQuotaAction<C>(int amount) : IAction<C> where C : IBotContext
{
    public void Execute(Event @event, C ctx) => ctx.DeductQuota(amount);
}

/// <summary>發一則固定內容的聊天訊息(開場白這類綁 transition 的訊息)。</summary>
public sealed class SendChatAction<C>(string content) : IAction<C> where C : IBotContext
{
    public void Execute(Event @event, C ctx) => ctx.Messenger.SendChat(content);
}
