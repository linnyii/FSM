using Fsm.Core;

namespace Fsm.Composite;


public sealed class CompositeState<C>(string id, FiniteStateMachine<C> subFsm, Func<C, string> initialResolver)
    : IState<C>
{
    public string Id { get; } = id;


    public void OnEntry(C ctx)
    {
        var startId = initialResolver(ctx);
        subFsm.Reset(startId);
        subFsm.Current.OnEntry(ctx);
    }

    public void OnExit(C ctx) => subFsm.Current.OnExit(ctx);

    public FireResult Handle(Event @event, C ctx) => subFsm.Fire(@event, ctx);
}
