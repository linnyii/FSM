using Fsm.Core;

namespace Bot;

public interface ITransitionFeature<C> where C : IBotContext
{
    IEnumerable<IGuard<C>> Guards() => [];
    IEnumerable<IAction<C>> Actions() => [];
}

public sealed class CommandFeature<C>(string keyword) : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IGuard<C>> Guards() => [new CommandIsGuard<C>(keyword)];
}

public sealed class AdminOnlyFeature<C> : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IGuard<C>> Guards() => [new IsAdminGuard<C>()];
}

public sealed class CostFeature<C>(int amount) : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IGuard<C>> Guards() => [new HasQuotaGuard<C>(amount)];
    public IEnumerable<IAction<C>> Actions() => [new DeductQuotaAction<C>(amount)];
}

public sealed class ReplyFeature<C>(string content) : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IAction<C>> Actions() => [new SendChatAction<C>(content)];
}

public sealed class WhenFeature<C>(Func<Event, C, bool> predicate) : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IGuard<C>> Guards() => [new PredicateGuard<C>(predicate)];
}

public sealed class DoFeature<C>(Action<Event, C> does) : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IAction<C>> Actions() => [new DelegateAction<C>(does)];
}
