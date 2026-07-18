using Bot;
using Fsm.Core;

namespace Application;

// Cost 天生綁定 guard(檢查額度)+action(扣額度),故保留為一個 feature,邏輯引用 CostPolicy。
internal sealed class CostFeature(int amount) : ITransitionFeature<BotContext>
{
    public IEnumerable<IGuard<BotContext>> Guards() =>
        [new PredicateGuard<BotContext>((_, ctx) => CostPolicy.HasQuota(ctx, amount))];

    public IEnumerable<IAction<BotContext>> Actions() =>
        [new DelegateAction<BotContext>((_, ctx) => CostPolicy.Deduct(ctx, amount))];
}
