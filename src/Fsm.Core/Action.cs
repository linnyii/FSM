namespace Fsm.Core;

public interface IAction<in C>
{
    void Execute(Event @event, C ctx);
}

public sealed class DelegateAction<C>(Action<Event, C> action) : IAction<C>
{
    public void Execute(Event @event, C ctx) => action(@event, ctx);
}

public sealed class NoOpAction<C> : IAction<C>
{
    public static readonly NoOpAction<C> Instance = new();
    public void Execute(Event @event, C ctx) { }
}

public sealed class ActionList<C>(params IAction<C>[] actions) : IAction<C>
{
    private readonly IReadOnlyList<IAction<C>> _actions = actions;

    public void Execute(Event @event, C ctx)
    {
        foreach (var action in _actions)
            action.Execute(@event, ctx);
    }
}
