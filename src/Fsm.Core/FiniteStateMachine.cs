namespace Fsm.Core;

public sealed class FiniteStateMachine<C>
{
    private readonly Dictionary<string, IState<C>> _states;
    private readonly List<Transition<C>> _transitions;

    public IState<C> Current { get; private set; }

    public FiniteStateMachine(
        IEnumerable<IState<C>> states,
        IEnumerable<Transition<C>> transitions,
        string initialStateId)
    {
        _states = states.ToDictionary(s => s.Id);
        _transitions = transitions.ToList();
        Current = Resolve(initialStateId);
    }

    private IState<C> Resolve(string id) =>
        _states.TryGetValue(id, out var state)
            ? state
            : throw new InvalidOperationException($"Unknown state id: '{id}'");

    public void Reset(string stateId) => Current = Resolve(stateId);

    public FireResult Fire(Event @event, C ctx)
    {
        if (Current.Handle(@event, ctx) == FireResult.Consumed)
            return FireResult.Consumed;

        var transition = _transitions.FirstOrDefault(t =>
            t.From == Current.Id &&
            t.On == @event.Name &&
            t.Guard.Test(@event, ctx));

        if (transition is null)
            return FireResult.NotConsumed;

        Current.OnExit(ctx);
        transition.Action.Execute(@event, ctx);
        Current = Resolve(transition.To);
        Current.OnEntry(ctx);
        return FireResult.Consumed;
    }
}
