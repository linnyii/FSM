namespace Fsm.Core;

/// <summary>
/// 統一驅動查表的狀態機。吃 event，照規則跑 <c>handle → exit → action → entry</c>。
/// 核心只認得 <see cref="IState{C}"/> 介面——不知道 current 是 leaf 還是 composite（OCP）。
/// </summary>
/// <typeparam name="C">Context 型別。核心只把它傳來傳去，不認得其欄位（設計第 9 節解法二）。</typeparam>
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

    /// <summary>
    /// 把當前狀態強制設為指定 id（不觸發 entry/exit）。供 CompositeState 進場時
    /// 依 resolver 選定初始子狀態用。<b>不</b>呼叫 onEntry——由呼叫端決定。
    /// </summary>
    public void Reset(string stateId) => Current = Resolve(stateId);

    /// <summary>
    /// 餵一個事件。順序：
    /// <list type="number">
    /// <item>current.Handle(event) —— 先響應（composite 會先讓內層試）。內層吃掉就攔下。</item>
    /// <item>否則查表 filter(from==current &amp;&amp; on==event.name &amp;&amp; guard.Test())。</item>
    /// <item>取宣告順序第一條：exit → action → current=to → entry，回 Consumed。</item>
    /// <item>都沒有 → NotConsumed（冒泡）。</item>
    /// </list>
    /// </summary>
    public FireResult Fire(Event @event, C ctx)
    {
        // 1. 先響應。composite 會把事件委派給內層先試；內層轉移了就 Consumed。
        //    這是「委派」非「遞迴」：composite 的 Handle 呼叫的是子 FSM 物件的 Fire（每層各自一台 FSM），
        //    不是同一台 FSM 自我遞迴。leaf 的 Handle 做完響應（輪播/累計）一定回 NotConsumed，落到下面查表。
        if (Current.Handle(@event, ctx) == FireResult.Consumed)
            return FireResult.Consumed;

        // 2. 內層沒吃到才查外層。三維度查表：from + on(name) + guard(細篩)。
        var transition = _transitions.FirstOrDefault(t =>
            t.From == Current.Id &&
            t.On == @event.Name &&
            t.Guard.Test(@event, ctx));

        // 3. 沒有 guard 通過的 transition → 靜默失敗、冒泡。
        if (transition is null)
            return FireResult.NotConsumed;

        // 4. exit → transition.action → 進新狀態 → entry（需求白紙黑字的順序）。
        Current.OnExit(ctx);
        transition.Action.Execute(@event, ctx);
        Current = Resolve(transition.To);
        Current.OnEntry(ctx);
        return FireResult.Consumed;
    }
}
