using Fsm.Core;

namespace Bot;

public interface ITransitionFeature<C> where C : IBotContext
{
    IEnumerable<IGuard<C>> Guards() => [];
    IEnumerable<IAction<C>> Actions() => [];
}

public sealed class WhenFeature<C>(Func<Event, C, bool> predicate) : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IGuard<C>> Guards() => [new PredicateGuard<C>(predicate)];
}

public sealed class DoFeature<C>(Action<Event, C> does) : ITransitionFeature<C> where C : IBotContext
{
    public IEnumerable<IAction<C>> Actions() => [new DelegateAction<C>(does)];
}
