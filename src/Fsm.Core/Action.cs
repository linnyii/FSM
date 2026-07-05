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

/// <summary>把多個 action 依序組成一個（bot DSL 的 .costs() 疊加扣額度 + 發訊息）。</summary>
public sealed class CompositeAction<C> : IAction<C>
{
    private readonly IReadOnlyList<IAction<C>> _actions;
    public CompositeAction(params IAction<C>[] actions) => _actions = actions;

    public void Execute(Event @event, C ctx)
    {
        foreach (var action in _actions)
            action.Execute(@event, ctx);
    }
}
