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
public sealed class DelegateAction<C>(Action<Event, C> action) : IAction<C>
{
    public void Execute(Event @event, C ctx) => action(@event, ctx);
}

/// <summary>什麼都不做——transition 未指定 action 時的預設。</summary>
public sealed class NoOpAction<C> : IAction<C>
{
    public static readonly NoOpAction<C> Instance = new();
    public void Execute(Event @event, C ctx) { }
}

public sealed class TransitionAction<C>(params IAction<C>[] actions) : IAction<C>
{
    private readonly IReadOnlyList<IAction<C>> _actions = actions;

    public void Execute(Event @event, C ctx)
    {
        foreach (var action in _actions)
            action.Execute(@event, ctx);
    }
}
