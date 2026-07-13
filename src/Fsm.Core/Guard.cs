namespace Fsm.Core;

public interface IGuard<in C>
{
    bool Test(Event @event, C ctx);
}

public sealed class AlwaysTrueGuard<C> : IGuard<C>
{
    public static readonly AlwaysTrueGuard<C> Instance = new();
    public bool Test(Event @event, C ctx) => true;
}

public sealed class PredicateGuard<C>(Func<Event, C, bool> predicate) : IGuard<C>
{
    public bool Test(Event @event, C ctx) => predicate(@event, ctx);
}

public sealed class GuardList<C>(params IGuard<C>[] guards) : IGuard<C>
{
    private readonly IReadOnlyList<IGuard<C>> _guards = guards;

    public bool Test(Event @event, C ctx)
    {
        foreach (var guard in _guards)
            if (!guard.Test(@event, ctx))
                return false;
        return true;
    }
}
