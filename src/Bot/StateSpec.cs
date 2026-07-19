using Fsm.Core;
using Fsm.Composite;

namespace Bot;

internal sealed class StateSpec<TContext>(string id) where TContext : IBotContext
{
    private string Id { get; } = id;
    public Rotate<TContext>? Rotate { get; set; }
    public Action<TContext>? OnEntry { get; set; }
    public Action<TContext>? OnExit { get; set; }
    public Action<Event, TContext>? OnHandle { get; set; }
    public BotBuilder<TContext>? SubStates { get; set; }
    public Func<TContext, string>? InitialResolver { get; set; }

    public IState<TContext> BuildState()
    {
        if (SubStates is not null)
        {
            var subFsm = SubStates.Build();
            var resolver = InitialResolver ?? (_ => subFsm.CurrentState.Id);
            return new CompositeState<TContext>(Id, subFsm, resolver);
        }

        var onEntry = Rotate is null ? OnEntry : Rotate.DecorateEntry(OnEntry);
        var onHandle = Rotate is null ? OnHandle : Rotate.DecorateHandle(OnHandle);

        return new LeafState<TContext>(Id, onEntry, OnExit, onHandle);
    }
}
