using Bot;

namespace Application;

internal static class CostPolicy
{
    public static bool HasQuota(BotContext ctx, int amount) => ctx.TokenQuota >= amount;

    public static void Deduct(BotContext ctx, int amount) => ctx.DeductQuota(amount);
}
