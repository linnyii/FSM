using Bot;
using Fsm.Core;

namespace Application;

internal static class BotFeatures
{
    public static CommandFeature<BotContext> Command(string keyword) => new(keyword);
    public static AdminOnlyFeature<BotContext> AdminOnly() => new();
    public static CostFeature<BotContext> Costs(int amount) => new(amount);
    public static ReplyFeature<BotContext> Reply(string content) => new(content);
    public static WhenFeature<BotContext> When(Func<Event, BotContext, bool> predicate) => new(predicate);
    public static DoFeature<BotContext> Do(Action<Event, BotContext> does) => new(does);
}
