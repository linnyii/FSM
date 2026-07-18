using Bot;
using Fsm.Core;

namespace Application;

internal static class BotFeatures
{
    public static WhenFeature<BotContext> Command(string keyword) =>
        new((e, ctx) => CommandPolicy.Is(e, ctx, keyword));
    public static WhenFeature<BotContext> AdminOnly() =>
        new((_, ctx) => AdminPolicy.IsAdmin(ctx));
    public static CostFeature Costs(int amount) => new(amount);
    public static DoFeature<BotContext> Reply(string content) =>
        new((_, ctx) => ctx.Messenger.SendChat(content));
    public static WhenFeature<BotContext> When(Func<Event, BotContext, bool> predicate) => new(predicate);
    public static DoFeature<BotContext> Do(Action<Event, BotContext> does) => new(does);
}
