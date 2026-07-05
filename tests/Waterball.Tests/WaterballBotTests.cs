using Application;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class WaterballBotTests
{
    private const int Admin = 1;
    private const int NonAdmin = 2;

    private static (FiniteStateMachine<BotContext> fsm, BotContext ctx, SpyMessenger spy) NewBot(int quota = 100)
    {
        var spy = new SpyMessenger();
        var ctx = new BotContext(spy, initialQuota: quota);
        var fsm = WaterballBot.Define();
        fsm.Current.OnEntry(ctx); // 啟動初始子狀態（Normal → Default）
        return (fsm, ctx, spy);
    }

    private static Event Msg(int authorId, string content, bool tagsBot = true) =>
        new(BotEvents.NewMessage, new ChatMessage(authorId, content, tagsBot));

    [Fact]
    public void King_by_admin_rotates_then_starts_deducts_quota_and_asks_question()
    {
        var (fsm, ctx, spy) = NewBot(quota: 10);
        ctx.IsCurrentUserAdmin = true;

        fsm.Fire(Msg(Admin, "king"), ctx);

        // 先響應（輪播 good to hear）→ 開場白（transition action）→ 出第 0 題（Questioning.entry）
        Assert.Equal(new[]
        {
            "chat:good to hear",
            "chat:KnowledgeKing is started!",
            "chat:Question 0",
        }, spy.Log);
        Assert.Equal(5, ctx.Quota);                 // 扣了 5
        Assert.Equal("KnowledgeKing", fsm.Current.Id);
    }

    [Fact]
    public void King_by_non_admin_silently_fails_but_rotation_still_fires()
    {
        var (fsm, ctx, spy) = NewBot(quota: 10);
        ctx.IsCurrentUserAdmin = false;

        var result = fsm.Fire(Msg(NonAdmin, "king"), ctx);

        Assert.Equal(FireResult.NotConsumed, result);
        Assert.Equal(new[] { "chat:good to hear" }, spy.Log); // 只有輪播，沒轉移
        Assert.Equal(10, ctx.Quota);                          // 沒扣
        Assert.Equal("Normal", fsm.Current.Id);
    }

    [Fact]
    public void King_with_insufficient_quota_silently_fails()
    {
        var (fsm, ctx, spy) = NewBot(quota: 3);
        ctx.IsCurrentUserAdmin = true;

        var result = fsm.Fire(Msg(Admin, "king"), ctx);

        Assert.Equal(FireResult.NotConsumed, result);
        Assert.Equal("Normal", fsm.Current.Id);
        Assert.Equal(3, ctx.Quota);
    }

    [Fact]
    public void Play_again_uses_its_own_opening_line_not_kings()
    {
        var (fsm, ctx, spy) = NewBot(quota: 10);
        ctx.IsCurrentUserAdmin = true;

        fsm.Fire(Msg(Admin, "king"), ctx);        // 進 KnowledgeKing/Questioning
        fsm.Fire(new Event(WaterballBot.Elapsed), ctx); // Q0 -> Q1
        fsm.Fire(new Event(WaterballBot.Elapsed), ctx); // Q1 -> Q2
        fsm.Fire(new Event(WaterballBot.Elapsed), ctx); // Q2 (last) -> ThanksForJoining
        spy.Log.Clear();

        fsm.Fire(Msg(Admin, "play again"), ctx);

        // play again 的開場白是 "gonna start again!"（因路徑而異），再出第 0 題（共同）
        Assert.Equal(new[]
        {
            "chat:KnowledgeKing is gonna start again!",
            "chat:Question 0",
        }, spy.Log);
        Assert.Equal("KnowledgeKing", fsm.Current.Id);
    }

    [Fact]
    public void King_stop_bubbles_from_inner_to_return_to_Normal()
    {
        var (fsm, ctx, _) = NewBot(quota: 100);
        ctx.IsCurrentUserAdmin = true;
        fsm.Fire(Msg(Admin, "king"), ctx);

        fsm.Fire(Msg(Admin, "king-stop"), ctx);

        Assert.Equal("Normal", fsm.Current.Id);
    }

    [Fact]
    public void Record_then_go_broadcasting_stays_in_record_and_broadcasts()
    {
        var (fsm, ctx, spy) = NewBot(quota: 100);
        fsm.Fire(Msg(NonAdmin, "record"), ctx);   // 進 Record（初始 Waiting，無人廣播）
        spy.Log.Clear();

        var result = fsm.Fire(new Event(WaterballBot.GoBroadcasting), ctx);

        Assert.Equal(FireResult.Consumed, result);
        Assert.Equal("Record", fsm.Current.Id);        // 內層轉 Recording，不冒泡
        Assert.True(ctx.SomeoneIsBroadcasting);
        Assert.Contains("go-broadcasting", spy.Log);   // Recording.entry
    }

    [Fact]
    public void Stop_recording_bubbles_to_leave_record_for_normal()
    {
        var (fsm, ctx, _) = NewBot(quota: 100);
        fsm.Fire(Msg(NonAdmin, "record"), ctx);
        fsm.Fire(new Event(WaterballBot.GoBroadcasting), ctx);

        var result = fsm.Fire(new Event(WaterballBot.StopRecording), ctx);

        Assert.Equal(FireResult.Consumed, result);
        Assert.Equal("Normal", fsm.Current.Id);
        Assert.False(ctx.SomeoneIsBroadcasting);
    }

    [Fact]
    public void Normal_resolver_picks_Interacting_when_crowded()
    {
        var (fsm, ctx, spy) = NewBot(quota: 100);
        ctx.OnlineCount = 15; // >= 10 → Interacting

        // login 是留在 Normal 的自我轉移，會重跑 Normal.OnEntry → resolver 重選子狀態。
        fsm.Fire(new Event(WaterballBot.Login), ctx);
        spy.Log.Clear();

        // Interacting 的輪播內容跟 Default 不同，用它驗證選到了 Interacting。
        fsm.Fire(Msg(NonAdmin, "hi"), ctx);
        Assert.Equal(new[] { "chat:nice to see you" }, spy.Log);
    }
}
