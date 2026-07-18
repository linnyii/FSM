using Fsm.Core;
using Fsm.Composite;

namespace Bot;

public sealed class BotBuilder<TContext> where TContext : IBotContext
{
    private readonly Dictionary<string, StateSpec> _states = new();
    private readonly List<Transition<TContext>> _transitions = [];
    private string? _initialStateId;

    
    public void AddLeafState(
        string id,
        string[]? botAutoRotateMessage = null,
        Action<TContext>? onEnter = null,
        Action<TContext>? onExit = null,
        Action<Event, TContext>? onHandle = null)
    {
        var spec = GetOrCreateTheState(id);
        spec.Rotate = botAutoRotateMessage is null ? null : new Rotate<TContext>(botAutoRotateMessage);
        spec.OnEntry = onEnter;
        spec.OnExit = onExit;
        spec.OnHandle = onHandle;
    }

    public BotBuilder<TContext> AddCompositeState(
        string id,
        Func<TContext, string>? initialLeafStateResolver = null)
    {
        var spec = GetOrCreateTheState(id);
        spec.SubStates = new BotBuilder<TContext>();
        spec.InitialResolver = initialLeafStateResolver;
        return spec.SubStates;
    }

    public void InitStateFrom(string id) => _initialStateId = id;

    public void AddTransition(
        string stateFrom,
        string triggerEventName,
        string stateTo,
        params ITransitionFeature<TContext>[] features)
    {
        var guards = features.SelectMany(f => f.Guards()).ToArray();
        var actions = features.SelectMany(f => f.Actions()).ToArray();

        IGuard<TContext> guard = guards.Length == 0 ? AlwaysTrueGuard<TContext>.Instance : new GuardList<TContext>(guards);
        var action = BuildAction(actions);
        _transitions.Add(new Transition<TContext>(stateFrom, triggerEventName, stateTo, guard, action));
    }

    public FiniteStateMachine<TContext> Build()
    {
        if (_initialStateId is null)
            throw new InvalidOperationException("Bot has no states.");

        var states = _states.Values.Select(spec => spec.BuildState()).ToList();
        return new FiniteStateMachine<TContext>(states, _transitions, _initialStateId);
    }

    private StateSpec GetOrCreateTheState(string id)
    {
        if (_states.TryGetValue(id, out var spec)) return spec;
        spec = new StateSpec(id);
        _states[id] = spec;
        _initialStateId ??= id; // 第一個宣告的狀態預設為初始狀態
        return spec;
    }

    private static IAction<TContext> BuildAction(IReadOnlyList<IAction<TContext>> actions) =>
        actions.Count switch
        {
            0 => NoOpAction<TContext>.Instance,
            1 => actions[0],
            _ => new ActionList<TContext>(actions.ToArray()),
        };

    private sealed class StateSpec(string id)
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
}
