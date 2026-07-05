namespace Fsm.Core;

/// <summary>
/// 純資料的轉移規則（全題靈魂）：<c>from + on + guard → action → to</c>。
/// 抽成一張清單交 FSM 統一查表 → 改轉移 = 改資料不改 State（OCP）。
/// <c>to</c> 用 stateId 而非 State 物件 → 避免建構順序依賴與循環引用。
/// </summary>
public sealed class Transition<C>
{
    public string From { get; }
    public string On { get; }
    public IGuard<C> Guard { get; }
    public IAction<C> Action { get; }
    public string To { get; }

    public Transition(
        string from,
        string on,
        string to,
        IGuard<C>? guard = null,
        IAction<C>? action = null)
    {
        From = from;
        On = on;
        To = to;
        Guard = guard ?? AlwaysTrueGuard<C>.Instance;
        Action = action ?? NoOpAction<C>.Instance;
    }
}
