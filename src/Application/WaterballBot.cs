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
            initialLeafStateResolver: ctx => ctx.OnlineCount < InteractingThreshold ? Default : Interacting);
        normal.AddLeafState(Default,
            botAutoRotateMessage: ["good to hear", "thank you", "How are you"]);
        normal.AddLeafState(Interacting,
            botAutoRotateMessage: ["nice to see you", "welcome back", "let's chat"]);

        bot.AddCommandTransition(
            stateFrom:      Normal,
            triggerCommandKey:   "king",
            adminOnly: true,
            tokenCosts:     5,
            replies:   "KnowledgeKing is started!",
            does:      (_, ctx) => ctx.CurrentQuestionIndex = 0,
            stateTo:        KnowledgeKing);

        bot.AddCommandTransition(
            stateFrom:    Normal,
            triggerCommandKey: "record",
            tokenCosts:   3,
            stateTo:      Record);

        bot.AddTransition(stateFrom: Normal, triggerEventName: Login, stateTo: Normal,
            does: (_, ctx) => ctx.OnlineCount++);
        bot.AddTransition(stateFrom: Normal, triggerEventName: Logout, stateTo: Normal,
            does: (_, ctx) => ctx.OnlineCount = Math.Max(0, ctx.OnlineCount - 1));
    }

    private static void DefineKnowledgeKing(BotBuilder<BotContext> bot)
    {
        var kk = bot.AddCompositeState(KnowledgeKing); // 無 resolver → 固定進第一個宣告的子狀態

        kk.AddLeafState(Questioning,
            onEnter: ctx => ctx.Messenger.SendChat($"Question {ctx.CurrentQuestionIndex}"));

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: Elapsed, stateTo: Questioning,
            when: (_, ctx) => ctx.CurrentQuestionIndex < TotalQuestions - 1,
            does: (_, ctx) => ctx.CurrentQuestionIndex++);
        
        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: Elapsed, stateTo: ThanksForJoining,
            when: (_, ctx) => ctx.CurrentQuestionIndex >= TotalQuestions - 1);

        kk.AddLeafState(ThanksForJoining,
            onEnter: ctx => ctx.Messenger.SendChat("Thanks for joining!"));
        
        kk.AddCommandTransition(
            stateFrom:    ThanksForJoining,
            triggerCommandKey: "play again",
            replies: "KnowledgeKing is gonna start again!",
            does:    (_, ctx) => ctx.CurrentQuestionIndex = 0,
            stateTo:      Questioning);

        bot.AddCommandTransition(stateFrom: KnowledgeKing, triggerCommandKey: "king-stop", adminOnly: true, stateTo: Normal);
    }

    private static void DefineRecord(BotBuilder<BotContext> bot)
    {
        var record = bot.AddCompositeState(Record,
            initialLeafStateResolver: ctx => ctx.SomeoneIsBroadcasting ? Recording : Waiting);

        record.AddLeafState(Waiting,
            onEnter: ctx => ctx.Messenger.SendChat("[Record] waiting for a broadcaster..."));
        record.AddLeafState(Recording,
            onEnter: ctx => ctx.Messenger.GoBroadcasting());

        record.AddTransition(stateFrom: Waiting, triggerEventName: GoBroadcasting, stateTo: Recording,
            does: (_, ctx) => ctx.SomeoneIsBroadcasting = true);

        bot.AddTransition(stateFrom: Record, triggerEventName: StopRecording, stateTo: Normal,
            does: (_, ctx) => ctx.SomeoneIsBroadcasting = false);
    }
}
