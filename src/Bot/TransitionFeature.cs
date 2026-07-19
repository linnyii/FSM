using Fsm.Core;

namespace Bot;

public interface ITransitionFeature<C> where C : IBotContext
{
    IGuard<C>? Guard => null;
    IAction<C>? Action => null;
}

public sealed class WhenFeature<C>(Func<Event, C, bool> predicate) : ITransitionFeature<C> where C : IBotContext
{
    public IGuard<C> Guard { get; } = new PredicateGuard<C>(predicate);
}

public sealed class DoFeature<C>(Action<Event, C> does) : ITransitionFeature<C> where C : IBotContext
{
    public IAction<C> Action { get; } = new DelegateAction<C>(does);
}
