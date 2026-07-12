using Application;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class WaterballBotTests
{
    private const string Admin = "1";
    private const string NonAdmin = "2";

    private static (FiniteStateMachine<BotContext> fsm, BotContext ctx, SpyMessenger spy) NewBot(int quota = 100)
    {
        var spy = new SpyMessenger();
        var ctx = new BotContext(spy, initialTokenQuota: quota);
        var fsm = WaterballBot.Define();
        fsm.Current.OnEntry(ctx); // 啟動初始子狀態（Normal → Default）
        return (fsm, ctx, spy);
    }

    private static Event Msg(string authorId, string content, bool tagsBot = true) =>
        new(BotEvents.NewMessage, new ChatMessage(authorId, content, tagsBot));

    [Fact]
    public void King_by_admin_rotates_then_starts_deducts_quota_and_asks_question()
    {
        var (fsm, ctx, spy) = NewBot(quota: 10);
        ctx.CurrentUser = new User(Admin, isAdmin: true);

        fsm.Fire(Msg(Admin, "king"), ctx);

        // 先響應（輪播 good to hear）→ 開場白（transition action）→ 出第 0 題 + 作答提示（Questioning.entry）
        var bank = new Application.Quiz.ChoiceQuizBank();
        Assert.Equal(new[]
        {
            "chat:good to hear",
            "chat:KnowledgeKing is started!",
            $"chat:{bank.GetTheQuestionAt(0)}",
            "chat:請 @bot 並回覆選項代號(A/B/C/D)作答",
        }, spy.Log);
        Assert.Equal(5, ctx.TokenQuota);                 // 扣了 5
        Assert.Equal("KnowledgeKing", fsm.Current.Id);
    }

    [Fact]
    public void King_by_non_admin_silently_fails_but_rotation_still_fires()
    {
        var (fsm, ctx, spy) = NewBot(quota: 10);
        ctx.CurrentUser = new User(NonAdmin, isAdmin: false);

        var result = fsm.Fire(Msg(NonAdmin, "king"), ctx);

        Assert.Equal(FireResult.NotConsumed, result);
        Assert.Equal(new[] { "chat:good to hear" }, spy.Log); // 只有輪播，沒轉移
        Assert.Equal(10, ctx.TokenQuota);                          // 沒扣
        Assert.Equal("Normal", fsm.Current.Id);
    }

    [Fact]
    public void King_with_insufficient_quota_silently_fails()
    {
        var (fsm, ctx, spy) = NewBot(quota: 3);
        ctx.CurrentUser = new User(Admin, isAdmin: true);

        var result = fsm.Fire(Msg(Admin, "king"), ctx);

        Assert.Equal(FireResult.NotConsumed, result);
        Assert.Equal("Normal", fsm.Current.Id);
        Assert.Equal(3, ctx.TokenQuota);
    }

    [Fact]
    public void Play_again_uses_its_own_opening_line_not_kings()
    {
        var (fsm, ctx, spy) = NewBot(quota: 10);
        ctx.CurrentUser = new User(Admin, isAdmin: true);

        fsm.Fire(Msg(Admin, "king"), ctx);   // 進 KnowledgeKing/Questioning(3 題)
        DriveTimeoutThroughAllQuestions(fsm, ctx); // 全部 20s timeout → ThanksForJoining
        Assert.Equal("KnowledgeKing", fsm.Current.Id);
        spy.Log.Clear();

        fsm.Fire(Msg(Admin, "play again"), ctx);

        // play again 的開場白是 "gonna start again!"，再出第 0 題 + 作答提示（共同）
        var bank = new Application.Quiz.ChoiceQuizBank();
        Assert.Equal(new[]
        {
            "chat:KnowledgeKing is gonna start again!",
            $"chat:{bank.GetTheQuestionAt(0)}",
            "chat:請 @bot 並回覆選項代號(A/B/C/D)作答",
        }, spy.Log);
        Assert.Equal("KnowledgeKing", fsm.Current.Id);
    }

    // 每題一個 20s elapsed:onHandle 先累計到 20 → 同一事件的 20s transition 立即跨題;走完全部題目到 ThanksForJoining。
    private static void DriveTimeoutThroughAllQuestions(FiniteStateMachine<BotContext> fsm, BotContext ctx)
    {
        var count = ctx.QuizBank.Count;
        for (var i = 0; i < count; i++)
            fsm.Fire(Elapsed(20), ctx);
    }

    private static Event Elapsed(int seconds) => new(BotEvents.Elapsed, seconds);

    [Fact]
    public void King_stop_bubbles_from_inner_to_return_to_Normal()
    {
        var (fsm, ctx, _) = NewBot(quota: 100);
        ctx.CurrentUser = new User(Admin, isAdmin: true);
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
        fsm.Fire(Msg(NonAdmin, "record"), ctx);   // NonAdmin 成為錄音者
        fsm.Fire(new Event(WaterballBot.GoBroadcasting), ctx);

        var result = fsm.Fire(Msg(NonAdmin, "stop-recording"), ctx); // 限錄音者的指令

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
