using Application;
using Application.Quiz;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class KnowledgeKingFlowTests
{
    private const string Admin = "1";
    private const string Alice = "10";
    private const string Bob = "20";

    private static readonly ChoiceQuizBank Bank = new();

    // 正解:第0題 A、第1題 C、第2題 A。
    private static string Correct(int index) => index switch { 0 => "A", 1 => "C", 2 => "A", _ => "?" };

    private static (FiniteStateMachine<BotContext> fsm, BotContext ctx, SpyMessenger spy) StartKing()
    {
        var spy = new SpyMessenger();
        var ctx = new BotContext(spy, initialTokenQuota: 100);
        var fsm = WaterballBot.Define();
        fsm.Current.OnEntry(ctx);

        // 建幾個玩家 + admin 進表(login 語意)。
        ctx.UpsertUser(Admin, isAdmin: true);
        ctx.UpsertUser(Alice, isAdmin: false);
        ctx.UpsertUser(Bob, isAdmin: false);

        ctx.CurrentUser = ctx.Users[Admin];
        fsm.Fire(Msg(Admin, "king"), ctx); // 進 KnowledgeKing/Questioning(Q0)
        spy.Log.Clear();
        return (fsm, ctx, spy);
    }

    private static Event Msg(string authorId, string content, bool tagsBot = true) =>
        new(BotEvents.NewMessage, new ChatMessage(authorId, content, tagsBot));

    private static Event Elapsed(int seconds) => new(BotEvents.Elapsed, seconds);

    // 先設當前發話者(guard 讀 CurrentUser 非必要,但答對計分讀 Users 表)。
    private static void Answer(FiniteStateMachine<BotContext> fsm, BotContext ctx, string author, string content, bool tagsBot = true)
    {
        ctx.CurrentUser = ctx.Users.TryGetValue(author, out var u) ? u : new User(author, false);
        fsm.Fire(Msg(author, content, tagsBot), ctx);
    }

    [Fact]
    public void First_correct_answer_scores_congrats_and_advances()
    {
        var (fsm, ctx, spy) = StartKing();

        Answer(fsm, ctx, Alice, Correct(0));

        Assert.Equal(1, ctx.Users[Alice].Score);
        Assert.Contains("chat:Congrats! you got the answer!", spy.Log);
        Assert.Contains($"chat:{Bank.GetTheQuestionAt(1)}", spy.Log); // 進到第 1 題
    }

    [Fact]
    public void Second_correct_answer_same_question_is_silent_and_unscored()
    {
        var (fsm, ctx, spy) = StartKing();
        Answer(fsm, ctx, Alice, Correct(0)); // Alice 首答 → 進 Q1
        spy.Log.Clear();

        // 已進 Q1,Bob 用 Q1 正解答對 → 計分;再一個人答同題應靜默。
        Answer(fsm, ctx, Bob, Correct(1));   // Bob 首答 Q1 → 進 Q2
        var bobScore = ctx.Users[Bob].Score;
        spy.Log.Clear();

        Answer(fsm, ctx, Alice, Correct(2)); // Alice 首答 Q2(最後一題)→ Thanks
        // 再有人答已離開 Questioning,不再計分。
        Answer(fsm, ctx, Bob, Correct(2));
        Assert.Equal(1, bobScore);
    }

    [Fact]
    public void Wrong_answer_is_silent_and_unscored()
    {
        var (fsm, ctx, spy) = StartKing();

        Answer(fsm, ctx, Alice, "B"); // Q0 正解是 A
        Assert.Equal(0, ctx.Users[Alice].Score);
        Assert.Empty(spy.Log);
        Assert.Null(ctx.FirstCorrectAnswerer);
    }

    [Fact]
    public void Answer_without_tagging_bot_is_silent()
    {
        var (fsm, ctx, spy) = StartKing();

        Answer(fsm, ctx, Alice, Correct(0), tagsBot: false);
        Assert.Equal(0, ctx.Users[Alice].Score);
        Assert.Empty(spy.Log);
    }

    [Fact]
    public void Timeout_20s_no_answer_advances_to_next_question()
    {
        var (fsm, ctx, spy) = StartKing();

        fsm.Fire(Elapsed(20), ctx); // 沒人答對,20s → 跨題
        Assert.Contains($"chat:{Bank.GetTheQuestionAt(1)}", spy.Log);
    }

    [Fact]
    public void Last_question_correct_enters_thanks_for_joining()
    {
        var (fsm, ctx, spy) = StartKing();
        Answer(fsm, ctx, Alice, Correct(0)); // Q0 → Q1
        Answer(fsm, ctx, Alice, Correct(1)); // Q1 → Q2
        spy.Log.Clear();

        Answer(fsm, ctx, Alice, Correct(2)); // Q2(最後)答對 → Thanks
        // Thanks.onEnter 用 Speak 公布(無人廣播);Alice 3 分為唯一最高。
        Assert.Contains("speak:The winner is 10", spy.Log);
    }

    [Fact]
    public void Game_timeout_1h_forces_thanks_for_joining()
    {
        var (fsm, ctx, spy) = StartKing();

        fsm.Fire(Elapsed(3600), ctx); // 全場 1h → 強制 Thanks
        Assert.Contains("speak:Tie!", spy.Log); // 無人答對 → Tie
    }

    [Fact]
    public void Result_uses_sendchat_when_someone_broadcasting()
    {
        var (fsm, ctx, spy) = StartKing();
        ctx.SomeoneIsBroadcasting = true;
        Answer(fsm, ctx, Alice, Correct(0));
        Answer(fsm, ctx, Alice, Correct(1));
        spy.Log.Clear();

        Answer(fsm, ctx, Alice, Correct(2)); // → Thanks
        Assert.Contains("chat:The winner is 10", spy.Log); // 有人廣播 → SendChat
    }

    [Fact]
    public void Tie_when_multiple_top_scores()
    {
        var (fsm, ctx, spy) = StartKing();
        Answer(fsm, ctx, Alice, Correct(0)); // Alice 1 分 → Q1
        Answer(fsm, ctx, Bob, Correct(1));   // Bob 1 分 → Q2
        spy.Log.Clear();

        fsm.Fire(Elapsed(20), ctx); // Q2 無人答 → Thanks;Alice=Bob=1 → Tie
        Assert.Contains("speak:Tie!", spy.Log);
    }

    [Fact]
    public void Play_again_resets_scores_and_index()
    {
        var (fsm, ctx, spy) = StartKing();
        Answer(fsm, ctx, Alice, Correct(0));
        Answer(fsm, ctx, Alice, Correct(1));
        Answer(fsm, ctx, Alice, Correct(2)); // → Thanks
        ctx.CurrentUser = ctx.Users[Admin];

        fsm.Fire(Msg(Admin, "play again"), ctx);

        Assert.Equal(0, ctx.CurrentQuestionIndex);
        Assert.Equal(0, ctx.Users[Alice].Score);
    }

    [Fact]
    public void Play_again_after_thanks_does_not_leak_thanks_timeout_into_new_questioning()
    {
        var (fsm, ctx, spy) = StartKing();
        fsm.Fire(Elapsed(3600), ctx); // → Thanks
        fsm.Fire(Elapsed(20), ctx);   // Thanks 累計到 20（若不清會殘留）
        // 回到 Normal 了;重新開一場。
        ctx.CurrentUser = ctx.Users[Admin];
        fsm.Fire(Msg(Admin, "king"), ctx); // 新一場 Questioning

        // 一個小 tick(< 20s):不該因殘留的 ElapsedSecondsInThanks 誤觸「Thanks 20s → Normal」。
        fsm.Fire(Elapsed(5), ctx);
        Assert.Equal("KnowledgeKing", fsm.Current.Id);
    }

    [Fact]
    public void Thanks_timeout_20s_returns_to_normal()
    {
        var (fsm, ctx, spy) = StartKing();
        fsm.Fire(Elapsed(3600), ctx); // → Thanks
        Assert.Equal("KnowledgeKing", fsm.Current.Id);

        fsm.Fire(Elapsed(20), ctx); // Thanks 20s → Normal
        Assert.Equal("Normal", fsm.Current.Id);
    }
}
