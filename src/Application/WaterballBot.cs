using Bot;
using Fsm.Core;

namespace Application;

public static class WaterballBot
{
    private const string Normal = "Normal";
    private const string Default = "Default";
    private const string Interacting = "Interacting";
    private const string KnowledgeKing = "KnowledgeKing";
    private const string Questioning = "Questioning";
    private const string ThanksForJoining = "ThanksForJoining";
    private const string Record = "Record";
    private const string Waiting = "Waiting";
    private const string Recording = "Recording";

    public const string Login = "login";
    public const string Logout = "logout";
    public const string GoBroadcasting = "go broadcasting";
    public const string StopRecording = "stop-recording";
    public const string Elapsed = "elapsed";

    private const int InteractingThreshold = 10;
    private const int TotalQuestions = 3;

    public static FiniteStateMachine<BotContext> Define()
    {
        var bot = new BotBuilder<BotContext>();

        DefineNormal(bot);
        DefineKnowledgeKing(bot);
        DefineRecord(bot);

        bot.StartAt(Normal);
        return bot.Build();
    }

    private static void DefineNormal(BotBuilder<BotContext> bot)
    {
        var normal = bot.AddCompositeState(Normal,
            initialResolver: ctx => ctx.OnlineCount < InteractingThreshold ? Default : Interacting);
        normal.AddLeafState(Default,
            rotate: ["good to hear", "thank you", "How are you"]);
        normal.AddLeafState(Interacting,
            rotate: ["nice to see you", "welcome back", "let's chat"]);

        bot.AddCommand(
            from:      Normal,
            keyword:   "king",
            adminOnly: true,
            costs:     5,
            replies:   "KnowledgeKing is started!",
            does:      (_, ctx) => ctx.CurrentQuestionIndex = 0,
            to:        KnowledgeKing);

        bot.AddCommand(
            from:    Normal,
            keyword: "record",
            costs:   3,
            to:      Record);

        bot.AddTransition(from: Normal, on: Login, to: Normal,
            does: (_, ctx) => ctx.OnlineCount++);
        bot.AddTransition(from: Normal, on: Logout, to: Normal,
            does: (_, ctx) => ctx.OnlineCount = Math.Max(0, ctx.OnlineCount - 1));
    }

    private static void DefineKnowledgeKing(BotBuilder<BotContext> bot)
    {
        var kk = bot.AddCompositeState(KnowledgeKing); // 無 resolver → 固定進第一個宣告的子狀態

        kk.AddLeafState(Questioning,
            onEnter: ctx => ctx.Messenger.SendChat($"Question {ctx.CurrentQuestionIndex}"));

        kk.AddTransition(
            from: Questioning, on: Elapsed, to: Questioning,
            when: (_, ctx) => ctx.CurrentQuestionIndex < TotalQuestions - 1,
            does: (_, ctx) => ctx.CurrentQuestionIndex++);
        
        kk.AddTransition(
            from: Questioning, on: Elapsed, to: ThanksForJoining,
            when: (_, ctx) => ctx.CurrentQuestionIndex >= TotalQuestions - 1);

        kk.AddLeafState(ThanksForJoining,
            onEnter: ctx => ctx.Messenger.SendChat("Thanks for joining!"));
        
        kk.AddCommand(
            from:    ThanksForJoining,
            keyword: "play again",
            replies: "KnowledgeKing is gonna start again!",
            does:    (_, ctx) => ctx.CurrentQuestionIndex = 0,
            to:      Questioning);

        bot.AddCommand(from: KnowledgeKing, keyword: "king-stop", adminOnly: true, to: Normal);
    }

    private static void DefineRecord(BotBuilder<BotContext> bot)
    {
        var record = bot.AddCompositeState(Record,
            initialResolver: ctx => ctx.SomeoneIsBroadcasting ? Recording : Waiting);

        record.AddLeafState(Waiting,
            onEnter: ctx => ctx.Messenger.SendChat("[Record] waiting for a broadcaster..."));
        record.AddLeafState(Recording,
            onEnter: ctx => ctx.Messenger.GoBroadcasting());

        record.AddTransition(from: Waiting, on: GoBroadcasting, to: Recording,
            does: (_, ctx) => ctx.SomeoneIsBroadcasting = true);

        bot.AddTransition(from: Record, on: StopRecording, to: Normal,
            does: (_, ctx) => ctx.SomeoneIsBroadcasting = false);
    }
}
