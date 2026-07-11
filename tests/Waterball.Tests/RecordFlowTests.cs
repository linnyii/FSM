using Application;
using Application.Parsing;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class RecordFlowTests
{
    private const string Recorder = "9";

    private static (FiniteStateMachine<BotContext> fsm, BotContext ctx, SpyMessenger spy) StartRecord()
    {
        var spy = new SpyMessenger();
        var ctx = new BotContext(spy, initialTokenQuota: 100);
        var fsm = WaterballBot.Define();
        fsm.Current.OnEntry(ctx);

        ctx.CurrentUser = new User(Recorder, isAdmin: false);
        fsm.Fire(new Event(BotEvents.NewMessage, new ChatMessage(Recorder, "record", tagsBot: true)), ctx);
        spy.Log.Clear();
        return (fsm, ctx, spy);
    }

    private static Event GoBroadcasting() => new(BotEvents.GoBroadcasting, new BroadcastInfo("4"));
    private static Event Speak(string content) => new(BotEvents.Speak, new SpeakInfo("4", content));
    private static Event StopBroadcasting() => new(BotEvents.StopBroadcasting, new BroadcastInfo("4"));

    [Fact]
    public void Recording_accumulates_speak_without_transition_or_output()
    {
        var (fsm, ctx, spy) = StartRecord();
        fsm.Fire(GoBroadcasting(), ctx); // Waiting → Recording
        spy.Log.Clear();

        var result = fsm.Fire(Speak("大家早安"), ctx);

        Assert.Equal(FireResult.NotConsumed, result); // 累積不轉移 → 冒泡靜默
        Assert.Equal("Record", fsm.Current.Id);
        Assert.Equal(new[] { "大家早安" }, ctx.RecordBuffer);
        Assert.Empty(spy.Log); // 無輸出
    }

    [Fact]
    public void Multiple_speaks_accumulate_in_order()
    {
        var (fsm, ctx, _) = StartRecord();
        fsm.Fire(GoBroadcasting(), ctx);

        fsm.Fire(Speak("一"), ctx);
        fsm.Fire(Speak("二"), ctx);
        fsm.Fire(Speak("三"), ctx);

        Assert.Equal(new[] { "一", "二", "三" }, ctx.RecordBuffer);
    }

    [Fact]
    public void Stop_broadcasting_emits_replay_clears_buffer_and_loops_to_waiting()
    {
        var (fsm, ctx, spy) = StartRecord();
        fsm.Fire(GoBroadcasting(), ctx);
        fsm.Fire(Speak("一"), ctx);
        fsm.Fire(Speak("二"), ctx);
        spy.Log.Clear();

        fsm.Fire(StopBroadcasting(), ctx);

        Assert.Contains("chat:[Record Replay] 一\n二", spy.Log); // 換行分隔 + @錄音者(tag 在 spy 不顯示但有傳)
        Assert.Empty(ctx.RecordBuffer);                          // buffer 清空
        Assert.Contains("chat:[Record] waiting for a broadcaster...", spy.Log); // 回 Waiting
        Assert.Equal("Record", fsm.Current.Id);
    }

    [Fact]
    public void Loop_back_to_waiting_then_record_again_starts_from_empty_buffer()
    {
        var (fsm, ctx, spy) = StartRecord();
        fsm.Fire(GoBroadcasting(), ctx);
        fsm.Fire(Speak("一"), ctx);
        fsm.Fire(StopBroadcasting(), ctx); // → Waiting,buffer 清空
        spy.Log.Clear();

        fsm.Fire(GoBroadcasting(), ctx);   // 再進 Recording
        Assert.Empty(ctx.RecordBuffer);
        fsm.Fire(Speak("二"), ctx);
        Assert.Equal(new[] { "二" }, ctx.RecordBuffer);
    }

    [Fact]
    public void Stop_recording_leaves_record_for_normal_from_any_substate()
    {
        var (fsm, ctx, _) = StartRecord();
        fsm.Fire(GoBroadcasting(), ctx); // 在 Recording

        var result = fsm.Fire(StopRecordingCmd(), ctx); // 限錄音者的指令

        Assert.Equal(FireResult.Consumed, result);
        Assert.Equal("Normal", fsm.Current.Id);
        Assert.False(ctx.SomeoneIsBroadcasting);
    }

    [Fact]
    public void Stop_recording_while_recording_emits_replay_then_normal()
    {
        var (fsm, ctx, spy) = StartRecord();
        fsm.Fire(GoBroadcasting(), ctx);
        fsm.Fire(Speak("一"), ctx);
        fsm.Fire(Speak("二"), ctx);
        spy.Log.Clear();

        fsm.Fire(StopRecordingCmd(), ctx);

        Assert.Contains("chat:[Record Replay] 一\n二", spy.Log); // 換行分隔 + @錄音者
        Assert.Empty(ctx.RecordBuffer);
        Assert.Equal("Normal", fsm.Current.Id);
    }

    [Fact]
    public void Stop_recording_while_waiting_emits_no_replay()
    {
        var (fsm, ctx, spy) = StartRecord(); // 停在 Waiting(沒 go broadcasting)
        spy.Log.Clear();

        fsm.Fire(StopRecordingCmd(), ctx);

        Assert.DoesNotContain(spy.Log, l => l.StartsWith("chat:[Record Replay]"));
        Assert.Equal("Normal", fsm.Current.Id);
    }

    // stop-recording 是「限錄音者」的指令(new message + tag bot + 內容);錄音者 = 下 record 的人。
    private static Event StopRecordingCmd() =>
        new(BotEvents.NewMessage, new ChatMessage(Recorder, "stop-recording", tagsBot: true));

    [Fact]
    public void Speak_in_other_state_is_silent_and_not_accumulated()
    {
        var spy = new SpyMessenger();
        var ctx = new BotContext(spy, initialTokenQuota: 100);
        var fsm = WaterballBot.Define();
        fsm.Current.OnEntry(ctx); // Normal，沒進 Record

        var result = fsm.Fire(Speak("路過"), ctx);

        Assert.Equal(FireResult.NotConsumed, result);
        Assert.Empty(ctx.RecordBuffer);
        Assert.Equal("Normal", fsm.Current.Id);
    }
}
