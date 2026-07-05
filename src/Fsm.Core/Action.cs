namespace Fsm.Core;

/// <summary>
/// 伴隨一條 transition 執行的副作用（扣額度、發訊息、reset 計數）。
/// 不做判斷（判斷是 Guard 的事），假設「輪到我執行時條件已成立」。
/// 發訊息透過注入的 Messenger 完成 → <see cref="Execute"/> 回 void（見設計輸出決策 A）。
/// </summary>
public interface IAction<in C>
{
    void Execute(Event @event, C ctx);
}

/// <summary>用委派快速組一個 action。</summary>
public sealed class DelegateAction<C> : IAction<C>
{
    private readonly Action<Event, C> _action;
    public DelegateAction(Action<Event, C> action) => _action = action;
    public void Execute(Event @event, C ctx) => _action(@event, ctx);
}

/// <summary>什麼都不做——transition 未指定 action 時的預設。</summary>
public sealed class NoOpAction<C> : IAction<C>
{
    public static readonly NoOpAction<C> Instance = new();
    public void Execute(Event @event, C ctx) { }
}

/// <summary>
/// 執行一條 transition 時要依序跑的一串 action（扣額度 + 發開場白 + 自訂副作用…）。
/// transition 的 action 欄位只裝一個 IAction,故用它把多個包成一個依序執行。
/// </summary>
public sealed class TransitionAction<C> : IAction<C>
{
    private readonly IReadOnlyList<IAction<C>> _actions;
    public TransitionAction(params IAction<C>[] actions) => _actions = actions;

    public void Execute(Event @event, C ctx)
    {
        foreach (var action in _actions)
            action.Execute(@event, ctx);
    }
}
