using Bot;
using Fsm.Core;
using static Application.BotFeatures;

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

    private const int InteractingThreshold = 10;

    public static FiniteStateMachine<BotContext> Define()
    {
        var bot = new BotBuilder<BotContext>();

        DefineNormalState(bot);
        DefineKnowledgeKingState(bot);
        DefineRecordState(bot);

        bot.StartAt(Normal);
        return bot.Build();
    }

    private static void DefineNormalState(BotBuilder<BotContext> bot)
    {
        var normal = bot.AddCompositeState(Normal,
            initialLeafStateResolver: ctx => ctx.OnlineCount < InteractingThreshold ? Default : Interacting);
        normal.AddLeafState(Default,
            botAutoRotateMessage: ["good to hear", "thank you", "How are you"]);
        normal.AddLeafState(Interacting,
            botAutoRotateMessage: ["nice to see you", "welcome back", "let's chat"]);

        bot.AddCommandTransition(Normal, "king", KnowledgeKing,
            AdminOnly(), Costs(5), Reply("KnowledgeKing is started!"),
            Do((_, ctx) => KnowledgeKingPolicy.ResetGame(ctx)));

        bot.AddCommandTransition(Normal, "record", Record,
            Costs(3), Do(RecordingPolicy.StartRecording));

        bot.AddTransition(Normal, BotEvents.Login, Normal,
            Do((_, ctx) => ctx.OnlineCount++));
        bot.AddTransition(Normal, BotEvents.Logout, Normal,
            Do((_, ctx) => ctx.OnlineCount = Math.Max(0, ctx.OnlineCount - 1)));
    }

    private static void DefineKnowledgeKingState(BotBuilder<BotContext> bot)
    {
        var kk = bot.AddCompositeState(KnowledgeKing); // 無 resolver → 固定進第一個宣告的子狀態

        kk.AddLeafState(Questioning,
            onEnter: KnowledgeKingPolicy.OnEnterQuestioning,
            onHandle: KnowledgeKingPolicy.AccumulateElapsed);

        kk.AddTransition(Questioning, BotEvents.Elapsed, ThanksForJoining,
            When((_, ctx) => KnowledgeKingPolicy.IsGameTimeout(ctx)));

        kk.AddTransition(Questioning, BotEvents.NewMessage, Questioning,
            When((e, ctx) => KnowledgeKingPolicy.IsFirstCorrectAnswer(e, ctx) && !KnowledgeKingPolicy.IsLastQuestion(ctx)),
            Do((e, ctx) => { KnowledgeKingPolicy.AwardFirstCorrect(e, ctx); ctx.CurrentQuestionIndex++; }));

        kk.AddTransition(Questioning, BotEvents.NewMessage, ThanksForJoining,
            When((e, ctx) => KnowledgeKingPolicy.IsFirstCorrectAnswer(e, ctx) && KnowledgeKingPolicy.IsLastQuestion(ctx)),
            Do(KnowledgeKingPolicy.AwardFirstCorrect));

        kk.AddTransition(Questioning, BotEvents.Elapsed, Questioning,
            When((_, ctx) => ctx.ElapsedSecondsInQuestion >= KnowledgeKingPolicy.QuestionTimeoutSeconds && !KnowledgeKingPolicy.IsLastQuestion(ctx)),
            Do((_, ctx) => ctx.CurrentQuestionIndex++));

        kk.AddTransition(Questioning, BotEvents.Elapsed, ThanksForJoining,
            When((_, ctx) => ctx.ElapsedSecondsInQuestion >= KnowledgeKingPolicy.QuestionTimeoutSeconds && KnowledgeKingPolicy.IsLastQuestion(ctx)));


        kk.AddLeafState(ThanksForJoining,
            onEnter: KnowledgeKingPolicy.OnEnterThanksForJoining,
            onHandle: (e, ctx) => ctx.ElapsedSecondsInThanks += KnowledgeKingPolicy.SecondsOf(e));

        kk.AddCommandTransition(ThanksForJoining, "play again", Questioning,
            Reply("KnowledgeKing is gonna start again!"),
            Do((_, ctx) => KnowledgeKingPolicy.ResetGame(ctx)));

        bot.AddCommandTransition(KnowledgeKing, "king-stop", Normal, AdminOnly());

        bot.AddTransition(KnowledgeKing, BotEvents.Elapsed, Normal,
            When((_, ctx) => ctx.ElapsedSecondsInThanks >= KnowledgeKingPolicy.ThanksTimeoutSeconds));
    }

    private static void DefineRecordState(BotBuilder<BotContext> bot)
    {
        var record = bot.AddCompositeState(Record,
            initialLeafStateResolver: ctx => ctx.SomeoneIsBroadcasting ? Recording : Waiting);

        record.AddLeafState(Waiting,
            onEnter: ctx => ctx.Messenger.SendChat("[Record] waiting for a broadcaster..."));

        record.AddLeafState(Recording,
            onEnter: ctx => ctx.Messenger.GoBroadcasting(),
            onHandle: RecordingPolicy.AccumulateSpeak);

        record.AddTransition(Waiting, BotEvents.GoBroadcasting, Recording,
            Do((_, ctx) => ctx.SomeoneIsBroadcasting = true));

        record.AddTransition(Recording, BotEvents.StopBroadcasting, Waiting,
            Do(RecordingPolicy.RecordReplay));

        bot.AddTransition(Record, BotEvents.NewMessage, Normal,
            When(RecordingPolicy.IsStopRecordingByCurrentRecorder),
            Do(RecordingPolicy.RecordReplayForStopRecordingLeave));
    }
}
