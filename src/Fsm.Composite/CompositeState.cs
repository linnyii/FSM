using Fsm.Core;

namespace Fsm.Composite;


public sealed class CompositeState<C>(string id, FiniteStateMachine<C> subFsm, Func<C, string> subFsmInitialResolver)
    : IState<C>
{
    public string Id { get; } = id;


    public void OnEntry(C ctx)
    {
        var startId = subFsmInitialResolver(ctx);
        subFsm.ResetCurrentState(startId);
        subFsm.CurrentState.OnEntry(ctx);
    }

    public void OnExit(C ctx) => subFsm.CurrentState.OnExit(ctx);

    public TriggerResult Handle(Event @event, C ctx) => subFsm.Process(@event, ctx);
}
