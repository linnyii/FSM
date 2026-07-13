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

        bot.AddCommandTransition(
            stateFrom:      Normal,
            triggerCommandKey:   "king",
            adminOnly: true,
            tokenCosts:     5,
            replies:   "KnowledgeKing is started!",
            tasksToDo:      (_, ctx) => KnowledgeKingPolicy.ResetGame(ctx),
            stateTo:        KnowledgeKing);

        bot.AddCommandTransition(
            stateFrom:    Normal,
            triggerCommandKey: "record",
            tokenCosts:   3,
            tasksToDo:         RecordingPolicy.StartRecording,
            stateTo:      Record);

        bot.AddTransition(stateFrom: Normal, triggerEventName: BotEvents.Login, stateTo: Normal,
            tasksToDo: (_, ctx) => ctx.OnlineCount++);
        bot.AddTransition(stateFrom: Normal, triggerEventName: BotEvents.Logout, stateTo: Normal,
            tasksToDo: (_, ctx) => ctx.OnlineCount = Math.Max(0, ctx.OnlineCount - 1));
    }

    private static void DefineKnowledgeKingState(BotBuilder<BotContext> bot)
    {
        var kk = bot.AddCompositeState(KnowledgeKing); // 無 resolver → 固定進第一個宣告的子狀態

        kk.AddLeafState(Questioning,
            onEnter: KnowledgeKingPolicy.OnEnterQuestioning,
            onHandle: KnowledgeKingPolicy.AccumulateElapsed);

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.Elapsed, stateTo: ThanksForJoining,
            preCondition: (_, ctx) => KnowledgeKingPolicy.IsGameTimeout(ctx));

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.NewMessage, stateTo: Questioning,
            preCondition: (e, ctx) => KnowledgeKingPolicy.IsFirstCorrectAnswer(e, ctx) && !KnowledgeKingPolicy.IsLastQuestion(ctx),
            tasksToDo: (e, ctx) => { KnowledgeKingPolicy.AwardFirstCorrect(e, ctx); ctx.CurrentQuestionIndex++; });

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.NewMessage, stateTo: ThanksForJoining,
            preCondition: (e, ctx) => KnowledgeKingPolicy.IsFirstCorrectAnswer(e, ctx) && KnowledgeKingPolicy.IsLastQuestion(ctx),
            tasksToDo: KnowledgeKingPolicy.AwardFirstCorrect);

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.Elapsed, stateTo: Questioning,
            preCondition: (_, ctx) => ctx.ElapsedSecondsInQuestion >= KnowledgeKingPolicy.QuestionTimeoutSeconds && !KnowledgeKingPolicy.IsLastQuestion(ctx),
            tasksToDo: (_, ctx) => ctx.CurrentQuestionIndex++);

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.Elapsed, stateTo: ThanksForJoining,
            preCondition: (_, ctx) => ctx.ElapsedSecondsInQuestion >= KnowledgeKingPolicy.QuestionTimeoutSeconds && KnowledgeKingPolicy.IsLastQuestion(ctx));


        kk.AddLeafState(ThanksForJoining,
            onEnter: KnowledgeKingPolicy.OnEnterThanksForJoining,
            onHandle: (e, ctx) => ctx.ElapsedSecondsInThanks += KnowledgeKingPolicy.SecondsOf(e));

        kk.AddCommandTransition(
            stateFrom:    ThanksForJoining,
            triggerCommandKey: "play again",
            replies: "KnowledgeKing is gonna start again!",
            tasksToDo:    (_, ctx) => KnowledgeKingPolicy.ResetGame(ctx),
            stateTo:      Questioning);

        bot.AddCommandTransition(stateFrom: KnowledgeKing, triggerCommandKey: "king-stop", adminOnly: true, stateTo: Normal);

        bot.AddTransition(
            stateFrom: KnowledgeKing, triggerEventName: BotEvents.Elapsed, stateTo: Normal,
            preCondition: (_, ctx) => ctx.ElapsedSecondsInThanks >= KnowledgeKingPolicy.ThanksTimeoutSeconds);
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

        record.AddTransition(stateFrom: Waiting, triggerEventName: BotEvents.GoBroadcasting, stateTo: Recording,
            tasksToDo: (_, ctx) => ctx.SomeoneIsBroadcasting = true);

        record.AddTransition(stateFrom: Recording, triggerEventName: BotEvents.StopBroadcasting, stateTo: Waiting,
            tasksToDo: RecordingPolicy.RecordReplay);

        bot.AddTransition(stateFrom: Record, triggerEventName: BotEvents.NewMessage, stateTo: Normal,
            preCondition: RecordingPolicy.IsStopRecordingByCurrentRecorder,
            tasksToDo: RecordingPolicy.RecordReplayForStopRecordingLeave);
    }
}
