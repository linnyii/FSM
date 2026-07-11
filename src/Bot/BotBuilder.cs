using Fsm.Core;
using Fsm.Composite;

namespace Bot;

public sealed class BotBuilder<TContext> where TContext : IBotContext
{
    private readonly Dictionary<string, StateSpec> _states = new();
    private readonly List<Transition<TContext>> _transitions = [];
    private string? _initialStateId;

    // ────────────────────────── 狀態宣告 ──────────────────────────

    /// <summary>
    /// 加一個普通（葉）狀態。
    /// </summary>
    /// <param name="id">狀態 id。</param>
    /// <param name="botAutoRotateMessage">輪播訊息清單（一組訊息輪流回、重進場歸零）。null = 沒有輪播。</param>
    /// <param name="onEnter">進場動作（例如出第一題）。</param>
    /// <param name="onExit">出場動作。</param>
    public void AddLeafState(
        string id,
        string[]? botAutoRotateMessage = null,
        Action<TContext>? onEnter = null,
        Action<TContext>? onExit = null,
        Action<Event, TContext>? onHandle = null)
    {
        var spec = GetOrCreateTheState(id);
        spec.Rotate = botAutoRotateMessage is null ? null : new Rotate<TContext>(botAutoRotateMessage);
        spec.OnEnter = onEnter;
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

    /// <summary>指定初始狀態（不指定則為第一個宣告的狀態）。</summary>
    public void StartAt(string id) => _initialStateId = id;

    // ────────────────────────── 轉移宣告 ──────────────────────────

    
    public void AddCommandTransition(
        string stateFrom,
        string triggerCommandKey,
        string stateTo,
        bool adminOnly = false,
        int tokenCosts = 0,
        string? replies = null,
        Action<Event, TContext>? does = null)
    {
        var guards = new List<IGuard<TContext>> { BotGuards.CommandIs<TContext>(triggerCommandKey) }; // 鐵律:tag bot + 內容 == keyword
        if (adminOnly)
            guards.Add(BotGuards.IsAdmin<TContext>());
        if (tokenCosts > 0)
            guards.Add(BotGuards.HasQuota<TContext>(tokenCosts));

        var action = BuildAction(tokenCosts, replies, does);
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
            ? new AndGuard<TContext>(baseGuard, BotGuards.HasQuota<TContext>(tokenCosts))
            : baseGuard;

        var action = BuildAction(tokenCosts, showingMessage, tasksToDo);
        _transitions.Add(new Transition<TContext>(stateFrom, triggerEventName, stateTo, guard, action));
    }

    // ────────────────────────── 收斂 ──────────────────────────

    /// <summary>把宣告收斂成一台可 fire 的機器人。</summary>
    public FiniteStateMachine<TContext> Build()
    {
        if (_initialStateId is null)
            throw new InvalidOperationException("Bot has no states.");

        var states = _states.Values.Select(spec => spec.BuildState()).ToList();
        return new FiniteStateMachine<TContext>(states, _transitions, _initialStateId);
    }

    // ────────────────────────── 內部 ──────────────────────────

    private StateSpec GetOrCreateTheState(string id)
    {
        if (_states.TryGetValue(id, out var spec)) return spec;
        spec = new StateSpec(id);
        _states[id] = spec;
        _initialStateId ??= id; // 第一個宣告的狀態預設為初始狀態
        return spec;
    }

    // 把 costs 扣除 + replies 發話 + does 自訂,依序組成一個 action（順序:扣→回話→自訂）。
    private static IAction<TContext> BuildAction(int costs, string? replies, Action<Event, TContext>? does)
    {
        var parts = new List<IAction<TContext>>();
        if (costs > 0)
            parts.Add(BotActions.DeductQuota<TContext>(costs));
        if (replies is not null)
            parts.Add(BotActions.SendChat<TContext>(replies));
        if (does is not null)
            parts.Add(new DelegateAction<TContext>(does));

        return parts.Count switch
        {
            0 => NoOpAction<TContext>.Instance,
            1 => parts[0],
            _ => new TransitionAction<TContext>(parts.ToArray()),
        };
    }

    // 一個狀態的建造資料;Build() 時才轉成 LeafState 或 CompositeState。
    private sealed class StateSpec
    {
        public string Id { get; }
        public Rotate<TContext>? Rotate { get; set; }
        public Action<TContext>? OnEnter { get; set; }
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
                // 未指定 resolver → 退化為固定進第一個宣告的子狀態（KnowledgeKing 搭便車）。
                var resolver = InitialResolver ?? (_ => subFsm.Current.Id);
                return new CompositeState<TContext>(Id, subFsm, resolver);
            }

            // 有輪播:handle 發下一則、entry 先歸零（重進場從第一則開始）。
            Action<TContext>? onEntry = OnEnter;
            Action<Event, TContext>? onHandle = OnHandle;
            if (Rotate is not null)
            {
                var rotate = Rotate;
                var userHandle = OnHandle;
                onHandle = userHandle is null
                    ? rotate.Emit
                    : (e, ctx) => { userHandle(e, ctx); rotate.Emit(e, ctx); };
                var userEntry = OnEnter;
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
