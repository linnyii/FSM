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

    public void StartAt(string id) => _initialStateId = id;

    public void AddCommandTransition(
        string stateFrom,
        string triggerCommandKey,
        string stateTo,
        bool adminOnly = false,
        int tokenCosts = 0,
        string? replies = null,
        Action<Event, TContext>? tasksToDo = null)
    {
        var guards = new List<IGuard<TContext>> { new CommandIsGuard<TContext>(triggerCommandKey) }; // 鐵律:tag bot + 內容 == keyword
        if (adminOnly)
            guards.Add(new IsAdminGuard<TContext>());
        if (tokenCosts > 0)
            guards.Add(new HasQuotaGuard<TContext>(tokenCosts));

        var action = BuildAction(tokenCosts, replies, tasksToDo);
        _transitions.Add(new Transition<TContext>(stateFrom, BotEvents.NewMessage, stateTo, new AndGuard<TContext>(guards.ToArray()), action));
    }
    
    public void AddTransition(
        string stateFrom,
        string triggerEventName,
        string stateTo,
        Func<Event, TContext, bool>? preCondition = null,
        int tokenCosts = 0,
        string? showingMessage = null,
        Action<Event, TContext>? tasksToDo = null)
    {
        IGuard<TContext> baseGuard = preCondition is null ? AlwaysTrueGuard<TContext>.Instance : new PredicateGuard<TContext>(preCondition);
        var guard = tokenCosts > 0
            ? new AndGuard<TContext>(baseGuard, new HasQuotaGuard<TContext>(tokenCosts))
            : baseGuard;

        var action = BuildAction(tokenCosts, showingMessage, tasksToDo);
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

    private static IAction<TContext> BuildAction(int costs, string? replies, Action<Event, TContext>? does)
    {
        var parts = new List<IAction<TContext>>();
        if (costs > 0)
            parts.Add(new DeductQuotaAction<TContext>(costs));
        if (replies is not null)
            parts.Add(new SendChatAction<TContext>(replies));
        if (does is not null)
            parts.Add(new DelegateAction<TContext>(does));

        return parts.Count switch
        {
            0 => NoOpAction<TContext>.Instance,
            1 => parts[0],
            _ => new TransitionAction<TContext>(parts.ToArray()),
        };
    }

    private sealed class StateSpec
    {
        public string Id { get; }
        public Rotate<TContext>? Rotate { get; set; }
        public Action<TContext>? OnEntry { get; set; }
        public Action<TContext>? OnExit { get; set; }
        public Action<Event, TContext>? OnHandle { get; set; }
        public BotBuilder<TContext>? SubStates { get; set; }
        public Func<TContext, string>? InitialResolver { get; set; }

        public StateSpec(string id) => Id = id;

        public IState<TContext> BuildState()
        {
            if (SubStates is not null)
            {
                var subFsm = SubStates.Build();
                var resolver = InitialResolver ?? (_ => subFsm.Current.Id);
                return new CompositeState<TContext>(Id, subFsm, resolver);
            }

            // 有輪播:handle 發下一則、entry 先歸零（重進場從第一則開始）。
            Action<TContext>? onEntry = OnEntry;
            Action<Event, TContext>? onHandle = OnHandle;
            if (Rotate is not null)
            {
                var rotate = Rotate;
                var userHandle = OnHandle;
                onHandle = userHandle is null
                    ? rotate.Emit
                    : (e, ctx) => { userHandle(e, ctx); rotate.Emit(e, ctx); };
                var userEntry = OnEntry;
                onEntry = ctx =>
                {
                    userEntry?.Invoke(ctx);
                    rotate.ResetOnEntry(ctx);
                };
            }

            return new LeafState<TContext>(Id, onEntry, OnExit, onHandle);
        }
    }
}
