using Bot;
using Fsm.Core;

namespace Application;

internal static class BotFeatures
{
    public static WhenFeature<BotContext> When(Func<Event, BotContext, bool> predicate) => new(predicate);
    public static DoFeature<BotContext> Do(Action<Event, BotContext> does) => new(does);
}
