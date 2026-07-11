namespace Fsm.Core;

/// <summary>
/// 一條 transition「能不能發生」的純判斷。回傳 false → transition 不發生（靜默失敗）。
/// 純判斷、無副作用（Strategy pattern，client 可無限擴充而 FSM 不改）。
/// </summary>
/// <typeparam name="C">Context 型別，FSM 核心不認得它的欄位（見設計第 9 節解法二）。</typeparam>
public interface IGuard<in C>
{
    bool Test(Event @event, C ctx);
}

/// <summary>永遠回傳 true 的 guard——transition 未指定 guard 時的預設。</summary>
public sealed class AlwaysTrueGuard<C> : IGuard<C>
{
    public static readonly AlwaysTrueGuard<C> Instance = new();
    public bool Test(Event @event, C ctx) => true;
}

/// <summary>用委派快速組一個 guard。</summary>
public sealed class PredicateGuard<C> : IGuard<C>
{
    private readonly Func<Event, C, bool> _predicate;
    public PredicateGuard(Func<Event, C, bool> predicate) => _predicate = predicate;
    public bool Test(Event @event, C ctx) => _predicate(@event, ctx);
}

/// <summary>
/// 把多個 guard 以 AND 組合（全過才過）。bot DSL 的 <c>.command().adminOnly().costs(5)</c>
/// 收集成一串 guard，用它組起來（取代舊的 <c>.And()</c> 鏈）。空清單 → 恆真。
/// </summary>
public sealed class AndGuard<C>(params IGuard<C>[] guards) : IGuard<C>
{
    private readonly IReadOnlyList<IGuard<C>> _guards = guards;

    public bool Test(Event @event, C ctx)
    {
        foreach (var guard in _guards)
            if (!guard.Test(@event, ctx))
                return false;
        return true;
    }
}
