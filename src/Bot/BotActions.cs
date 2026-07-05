using Fsm.Core;

namespace Bot;

/// <summary>bot module 內建、可重用的 action。</summary>
internal static class BotActions
{
    /// <summary>額度橫切（扣除）：扣掉 n。與 <c>HasQuota(n)</c> 由 .costs(n) 配對成原子操作。</summary>
    public static IAction<C> DeductQuota<C>(int amount) where C : IBotContext =>
        new DelegateAction<C>((_, ctx) => ctx.DeductQuota(amount));

    /// <summary>發一則固定內容的聊天訊息（開場白這類綁 transition 的訊息）。</summary>
    public static IAction<C> SendChat<C>(string content) where C : IBotContext =>
        new DelegateAction<C>((_, ctx) => ctx.Messenger.SendChat(content));
}
