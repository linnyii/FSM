using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class FiniteStateMachineTests
{
    // 用一個 list 當黑板記錄呼叫順序。
    private sealed class TraceCtx
    {
        public List<string> Trace { get; } = new();
    }

    private static LeafState<TraceCtx> TracingState(string id) =>
        new(id,
            onEntry: ctx => ctx.Trace.Add($"{id}.entry"),
            onExit: ctx => ctx.Trace.Add($"{id}.exit"),
            onHandle: (_, ctx) => ctx.Trace.Add($"{id}.handle"));

    [Fact]
    public void Fire_runs_handle_then_exit_then_action_then_entry()
    {
        var ctx = new TraceCtx();
        var a = TracingState("A");
        var b = TracingState("B");
        var t = new Transition<TraceCtx>("A", "go", "B",
            action: new DelegateAction<TraceCtx>((_, c) => c.Trace.Add("action")));
        var fsm = new FiniteStateMachine<TraceCtx>(new[] { a, b }, new[] { t }, "A");

        var result = fsm.Fire(new Event("go"), ctx);

        Assert.Equal(FireResult.Consumed, result);
        Assert.Equal(new[] { "A.handle", "A.exit", "action", "B.entry" }, ctx.Trace);
        Assert.Equal("B", fsm.Current.Id);
    }

    [Fact]
    public void Handle_runs_even_when_guard_blocks_transition_silent_fail()
    {
        var ctx = new TraceCtx();
        var a = TracingState("A");
        var b = TracingState("B");
        // guard 永遠 false → transition 不發生（靜默失敗），但 handle 照跑。
        var t = new Transition<TraceCtx>("A", "go", "B",
            guard: new PredicateGuard<TraceCtx>((_, _) => false));
        var fsm = new FiniteStateMachine<TraceCtx>(new[] { a, b }, new[] { t }, "A");

        var result = fsm.Fire(new Event("go"), ctx);

        Assert.Equal(FireResult.NotConsumed, result);
        Assert.Equal(new[] { "A.handle" }, ctx.Trace); // 只有 handle，沒轉移
        Assert.Equal("A", fsm.Current.Id);
    }

    [Fact]
    public void Fire_picks_first_matching_transition_by_declaration_order()
    {
        var ctx = new TraceCtx();
        var a = new LeafState<TraceCtx>("A");
        var b = new LeafState<TraceCtx>("B");
        var c = new LeafState<TraceCtx>("C");
        // 兩條同 (from, on) 都 guard 通過 → 取宣告順序第一條（策略 B）。
        var first = new Transition<TraceCtx>("A", "go", "B");
        var second = new Transition<TraceCtx>("A", "go", "C");
        var fsm = new FiniteStateMachine<TraceCtx>(new[] { a, b, c }, new[] { first, second }, "A");

        fsm.Fire(new Event("go"), ctx);

        Assert.Equal("B", fsm.Current.Id);
    }

    [Fact]
    public void Guard_is_fine_filter_selects_the_matching_command()
    {
        var ctx = new TraceCtx();
        var a = new LeafState<TraceCtx>("A");
        var b = new LeafState<TraceCtx>("B");
        var c = new LeafState<TraceCtx>("C");
        // 同 (from, on) 兩候選，guard 依 payload 細篩。
        var toB = new Transition<TraceCtx>("A", "msg", "B",
            guard: new PredicateGuard<TraceCtx>((e, _) => (string?)e.Payload == "king"));
        var toC = new Transition<TraceCtx>("A", "msg", "C",
            guard: new PredicateGuard<TraceCtx>((e, _) => (string?)e.Payload == "record"));
        var fsm = new FiniteStateMachine<TraceCtx>(new[] { a, b, c }, new[] { toB, toC }, "A");

        fsm.Fire(new Event("msg", "record"), ctx);

        Assert.Equal("C", fsm.Current.Id);
    }
}
