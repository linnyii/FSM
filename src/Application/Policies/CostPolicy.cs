using Bot;
using Fsm.Core;

namespace Application;

internal static class CostPolicy
{
    public static Func<Event, BotContext, bool> HasQuota(int amount) =>
        (_, ctx) => ctx.TokenQuota >= amount;

    public static Action<Event, BotContext> DeductQuota(int amount) =>
        (_, ctx) => ctx.DeductQuota(amount);
}
