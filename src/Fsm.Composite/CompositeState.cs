using Fsm.Core;

namespace Fsm.Composite;

/// <summary>
/// 子狀態機 plugin：對外是一個 <see cref="IState{C}"/>、對內持有一台完整
/// <see cref="FiniteStateMachine{C}"/>（Composite pattern）。主 FSM 一行不改就支援任意深度巢狀。
/// <b>implements State + has-a FSM</b>，不是 extends FiniteStateMachine。
/// </summary>
public sealed class CompositeState<C> : IState<C>
{
    private readonly FiniteStateMachine<C> _subFsm;
    private readonly Func<C, string> _initialResolver;

    public string Id { get; }

    /// <param name="initialResolver">
    /// 進場當下依 ctx 決定初始子狀態（Record 看有無廣播、Normal 看線上人數）。
    /// 「會變的領域判斷」萃取成注入點——框架只看到 <c>(C)=&gt;stateId</c>，不知「廣播」是什麼。
    /// 初始固定的狀態（KnowledgeKing→Questioning）給退化 resolver <c>_ =&gt; "Questioning"</c> 搭便車。
    /// </param>
    public CompositeState(string id, FiniteStateMachine<C> subFsm, Func<C, string> initialResolver)
    {
        Id = id;
        _subFsm = subFsm;
        _initialResolver = initialResolver;
    }

    /// <summary>進場：resolver 決定初始子狀態，reset 進去，再觸發該子狀態的 entry。</summary>
    public void OnEntry(C ctx)
    {
        var startId = _initialResolver(ctx);
        _subFsm.Reset(startId);
        _subFsm.Current.OnEntry(ctx);
    }

    /// <summary>出場：收尾內部 FSM 的當前子狀態。</summary>
    public void OnExit(C ctx) => _subFsm.Current.OnExit(ctx);

    /// <summary>
    /// 委派內層先試（標準 HSM 語意）：內層轉移了 → Consumed（攔下）；
    /// 內層沒吃到 → NotConsumed（冒泡，主 FSM 自己查外層 transition）。
    /// 冒泡邏輯 100% 住在這裡，核心裡沒有任何 <c>if (composite)</c>。
    /// </summary>
    public FireResult Handle(Event @event, C ctx) => _subFsm.Fire(@event, ctx);
}
