namespace Fsm.Core;

public sealed class FiniteStateMachine<C>
{
    private readonly Dictionary<string, IState<C>> _states;
    private readonly List<Transition<C>> _transitions;

    public IState<C> CurrentState { get; private set; }

    public FiniteStateMachine(
        IEnumerable<IState<C>> states,
        IEnumerable<Transition<C>> transitions,
        string initialStateId)
    {
        _states = states.ToDictionary(s => s.Id);
        _transitions = transitions.ToList();
        CurrentState = Resolve(initialStateId);
    }

    private IState<C> Resolve(string id) =>
        _states.TryGetValue(id, out var state)
            ? state
            : throw new InvalidOperationException($"Unknown state id: '{id}'");

    public void ResetCurrentState(string stateId) => CurrentState = Resolve(stateId);

    public TriggerResult Process(Event @event, C ctx)
    {
        if (CurrentState.Handle(@event, ctx) == TriggerResult.Consumed)
            return TriggerResult.Consumed;

        var transition = _transitions.FirstOrDefault(t =>
            t.From == CurrentState.Id &&
            t.On == @event.Name &&
            t.Guard.Test(@event, ctx));

        if (transition is null)
            return TriggerResult.NotConsumed;

        CurrentState.OnExit(ctx);
        transition.Action.Execute(@event, ctx);
        CurrentState = Resolve(transition.To);
        CurrentState.OnEntry(ctx);
        return TriggerResult.Consumed;
    }
}
