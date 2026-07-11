using Fsm.Core;
using Fsm.Composite;

namespace Bot;

public sealed class BotBuilder<C> where C : IBotContext
{
    private readonly Dictionary<string, StateSpec> _states = new();
    private readonly List<Transition<C>> _transitions = [];
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
        Action<C>? onEnter = null,
        Action<C>? onExit = null,
        Action<Event, C>? onHandle = null)
    {
        var spec = GetOrCreateTheState(id);
        spec.Rotate = botAutoRotateMessage is null ? null : new Rotate<C>(botAutoRotateMessage);
        spec.OnEnter = onEnter;
        spec.OnExit = onExit;
        spec.OnHandle = onHandle;
    }

    public BotBuilder<C> AddCompositeState(
        string id,
        Func<C, string>? initialLeafStateResolver = null)
    {
        var spec = GetOrCreateTheState(id);
        spec.SubStates = new BotBuilder<C>();
        spec.InitialResolver = initialLeafStateResolver;
        return spec.SubStates;
    }

    /// <summary>指定初始狀態（不指定則為第一個宣告的狀態）。</summary>
    public void StartAt(string id) => _initialStateId = id;

    // ────────────────────────── 轉移宣告 ──────────────────────────

    /// <summary>
    /// 加一條「指令」轉移。內建 Waterball 鐵律:on = "new message"、guard = tag bot 且內容 == keyword。
    /// </summary>
    /// <param name="stateFrom">來源狀態 id。</param>
    /// <param name="triggerCommandKey">指令關鍵字（例如 "king"）。</param>
    /// <param name="stateTo">目標狀態 id。</param>
    /// <param name="adminOnly">是否只有管理員能用（疊 IsAdmin guard）。</param>
    /// <param name="tokenCosts">額度成本;&gt;0 時原子地同時疊 guard(檢查夠不夠) + action(扣除)。</param>
    /// <param name="replies">命中時額外發的一句話（開場白這類綁 transition 的訊息）。</param>
    /// <param name="does">命中時額外做的自訂副作用。</param>
    public void AddCommandTransition(
        string stateFrom,
        string triggerCommandKey,
        string stateTo,
        bool adminOnly = false,
        int tokenCosts = 0,
        string? replies = null,
        Action<Event, C>? does = null)
    {
        IGuard<C> guard = BotGuards.CommandIs<C>(triggerCommandKey); // 鐵律:tag bot + 內容 == keyword
        if (adminOnly)
            guard = guard.And(BotGuards.IsAdmin<C>());
        if (tokenCosts > 0)
            guard = guard.And(BotGuards.HasQuota<C>(tokenCosts));

        var action = BuildAction(tokenCosts, replies, does);
        _transitions.Add(new Transition<C>(stateFrom, BotEvents.NewMessage, stateTo, guard, action));
    }

    /// <summary>
    /// 加一條「非指令」轉移（login / go broadcasting / elapsed / stop-recording / 自動回…）。
    /// 以事件名觸發,不套指令鐵律;條件靠 <paramref name="when"/> 自己給。
    /// </summary>
    /// <param name="stateFrom">來源狀態 id。</param>
    /// <param name="triggerEventName">事件名（例如 "login"）。</param>
    /// <param name="stateTo">目標狀態 id。</param>
    /// <param name="when">額外 guard（null = 永遠成立）。</param>
    /// <param name="costs">額度成本;&gt;0 時同時疊檢查 + 扣除。</param>
    /// <param name="replies">命中時額外發的一句話。</param>
    /// <param name="does">命中時額外做的自訂副作用。</param>
    public void AddTransition(
        string stateFrom,
        string triggerEventName,
        string stateTo,
        Func<Event, C, bool>? when = null,
        int costs = 0,
        string? replies = null,
        Action<Event, C>? does = null)
    {
        IGuard<C> guard = when is null ? AlwaysTrueGuard<C>.Instance : new PredicateGuard<C>(when);
        if (costs > 0)
            guard = guard.And(BotGuards.HasQuota<C>(costs));

        var action = BuildAction(costs, replies, does);
        _transitions.Add(new Transition<C>(stateFrom, triggerEventName, stateTo, guard, action));
    }

    // ────────────────────────── 收斂 ──────────────────────────

    /// <summary>把宣告收斂成一台可 fire 的機器人。</summary>
    public FiniteStateMachine<C> Build()
    {
        if (_initialStateId is null)
            throw new InvalidOperationException("Bot has no states.");

        var states = _states.Values.Select(spec => spec.BuildState()).ToList();
        return new FiniteStateMachine<C>(states, _transitions, _initialStateId);
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
    private static IAction<C> BuildAction(int costs, string? replies, Action<Event, C>? does)
    {
        var parts = new List<IAction<C>>();
        if (costs > 0)
            parts.Add(BotActions.DeductQuota<C>(costs));
        if (replies is not null)
            parts.Add(BotActions.SendChat<C>(replies));
        if (does is not null)
            parts.Add(new DelegateAction<C>(does));

        return parts.Count switch
        {
            0 => NoOpAction<C>.Instance,
            1 => parts[0],
            _ => new TransitionAction<C>(parts.ToArray()),
        };
    }

    // 一個狀態的建造資料;Build() 時才轉成 LeafState 或 CompositeState。
    private sealed class StateSpec
    {
        public string Id { get; }
        public Rotate<C>? Rotate { get; set; }
        public Action<C>? OnEnter { get; set; }
        public Action<C>? OnExit { get; set; }
        public Action<Event, C>? OnHandle { get; set; }
        public BotBuilder<C>? SubStates { get; set; }
        public Func<C, string>? InitialResolver { get; set; }

        public StateSpec(string id) => Id = id;

        public IState<C> BuildState()
        {
            if (SubStates is not null)
            {
                var subFsm = SubStates.Build();
                // 未指定 resolver → 退化為固定進第一個宣告的子狀態（KnowledgeKing 搭便車）。
                var resolver = InitialResolver ?? (_ => subFsm.Current.Id);
                return new CompositeState<C>(Id, subFsm, resolver);
            }

            // 有輪播:handle 發下一則、entry 先歸零（重進場從第一則開始）。
            Action<C>? onEntry = OnEnter;
            Action<Event, C>? onHandle = OnHandle;
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

            return new LeafState<C>(Id, onEntry, OnExit, onHandle);
        }
    }
}
