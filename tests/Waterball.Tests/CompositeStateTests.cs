using Fsm.Core;
using Fsm.Composite;
using Xunit;

namespace Waterball.Tests;

public class CompositeStateTests
{
    private sealed class Ctx
    {
        public bool StartInSecond { get; set; }
        public List<string> Trace { get; } = new();
    }

    private static LeafState<Ctx> Leaf(string id) =>
        new(id,
            onEntry: ctx => ctx.Trace.Add($"{id}.entry"),
            onExit: ctx => ctx.Trace.Add($"{id}.exit"));

    // 內層：Waiting --go--> Recording
    private static FiniteStateMachine<Ctx> BuildSubFsm() =>
        new(new[] { Leaf("Waiting"), Leaf("Recording") },
            new[] { new Transition<Ctx>("Waiting", "go", "Recording") },
            "Waiting");

    [Fact]
    public void Inner_transition_is_consumed_and_does_not_bubble()
    {
        var ctx = new Ctx();
        var record = new CompositeState<Ctx>("Record", BuildSubFsm(), _ => "Waiting");
        var normal = Leaf("Normal");
        // 外層有一條 Record --go--> Normal，若冒泡就會誤觸。
        var outer = new Transition<Ctx>("Record", "go", "Normal");
        var fsm = new FiniteStateMachine<Ctx>(new IState<Ctx>[] { record, normal },
            new[] { outer }, "Record");
        fsm.Current.OnEntry(ctx);

        var result = fsm.Fire(new Event("go"), ctx);

        Assert.Equal(FireResult.Consumed, result);
        Assert.Equal("Record", fsm.Current.Id); // 內層吃掉，外層沒轉
    }

    [Fact]
    public void Unhandled_inner_event_bubbles_to_outer_transition()
    {
        var ctx = new Ctx();
        var record = new CompositeState<Ctx>("Record", BuildSubFsm(), _ => "Waiting");
        var normal = Leaf("Normal");
        // stop 內層沒有 → 冒泡到外層 Record --stop--> Normal
        var outer = new Transition<Ctx>("Record", "stop", "Normal");
        var fsm = new FiniteStateMachine<Ctx>(new IState<Ctx>[] { record, normal },
            new[] { outer }, "Record");
        fsm.Current.OnEntry(ctx);

        var result = fsm.Fire(new Event("stop"), ctx);

        Assert.Equal(FireResult.Consumed, result);
        Assert.Equal("Normal", fsm.Current.Id);
    }

    [Fact]
    public void Resolver_picks_initial_substate_from_context()
    {
        var ctx = new Ctx { StartInSecond = true };
        var record = new CompositeState<Ctx>("Record", BuildSubFsm(),
            c => c.StartInSecond ? "Recording" : "Waiting");
        var fsm = new FiniteStateMachine<Ctx>(new IState<Ctx>[] { record },
            Array.Empty<Transition<Ctx>>(), "Record");

        fsm.Current.OnEntry(ctx);

        // 進場時 resolver 選 Recording，並觸發其 entry。
        Assert.Contains("Recording.entry", ctx.Trace);
        Assert.DoesNotContain("Waiting.entry", ctx.Trace);
    }
}
