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

/// <summary>Guard 的可組合性：<c>.And()</c> / <c>.Or()</c>（bot DSL 的 .when(admin).costs(5) 翻成 AND）。</summary>
public static class GuardExtensions
{
    public static IGuard<C> And<C>(this IGuard<C> left, IGuard<C> right) =>
        new PredicateGuard<C>((e, ctx) => left.Test(e, ctx) && right.Test(e, ctx));

    public static IGuard<C> Or<C>(this IGuard<C> left, IGuard<C> right) =>
        new PredicateGuard<C>((e, ctx) => left.Test(e, ctx) || right.Test(e, ctx));
}
