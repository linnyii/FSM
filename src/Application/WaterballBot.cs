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
            does:      (_, ctx) => ResetGame(ctx),
            stateTo:        KnowledgeKing);

        bot.AddCommandTransition(
            stateFrom:    Normal,
            triggerCommandKey: "record",
            tokenCosts:   3,
            does:         StartRecording,
            stateTo:      Record);

        bot.AddTransition(stateFrom: Normal, triggerEventName: Login, stateTo: Normal,
            does: (_, ctx) => ctx.OnlineCount++);
        bot.AddTransition(stateFrom: Normal, triggerEventName: Logout, stateTo: Normal,
            does: (_, ctx) => ctx.OnlineCount = Math.Max(0, ctx.OnlineCount - 1));
    }

    private const int QuestionTimeoutSeconds = 20;
    private const int GameTimeoutSeconds = 3600;
    private const int ThanksTimeoutSeconds = 20;

    private static void DefineKnowledgeKingState(BotBuilder<BotContext> bot)
    {
        var kk = bot.AddCompositeState(KnowledgeKing); // 無 resolver → 固定進第一個宣告的子狀態

        // onHandle 在 transition 表之前跑:每個 elapsed 先累計,20s guard 才看得到最新秒數。
        // 用 onHandle(非 self-loop transition)累計 → 不觸發 re-entry、不會被 onEnter 歸零。
        kk.AddLeafState(Questioning, onEnter: OnEnterQuestioning, onHandle: AccumulateElapsed);

        // ── 全場 1h 到:強制進 ThanksForJoining(宣告在 20s 之前 → 優先於 20s 跨題) ──
        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.Elapsed, stateTo: ThanksForJoining,
            when: (_, ctx) => IsGameTimeout(ctx));

        // ── 答對:new message + tag bot + 判對 + 本題尚無人答對(首答) ──
        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.NewMessage, stateTo: Questioning,
            when: (e, ctx) => IsFirstCorrectAnswer(e, ctx) && !IsLastQuestion(ctx),
            does: (e, ctx) => { AwardFirstCorrect(e, ctx); ctx.CurrentQuestionIndex++; });

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.NewMessage, stateTo: ThanksForJoining,
            when: (e, ctx) => IsFirstCorrectAnswer(e, ctx) && IsLastQuestion(ctx),
            does: AwardFirstCorrect);

        // ── 20s 到,沒答對也跨題(1h transition 已宣告在前 → 這裡不必再排除 game timeout) ──
        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.Elapsed, stateTo: Questioning,
            when: (_, ctx) => ctx.ElapsedSecondsInQuestion >= QuestionTimeoutSeconds && !IsLastQuestion(ctx),
            does: (_, ctx) => ctx.CurrentQuestionIndex++);

        kk.AddTransition(
            stateFrom: Questioning, triggerEventName: BotEvents.Elapsed, stateTo: ThanksForJoining,
            when: (_, ctx) => ctx.ElapsedSecondsInQuestion >= QuestionTimeoutSeconds && IsLastQuestion(ctx));

        // 累計靠 Questioning 的 onHandle(見上),不用 self-loop transition。

        kk.AddLeafState(ThanksForJoining, onEnter: OnEnterThanksForJoining,
            onHandle: (e, ctx) => ctx.ElapsedSecondsInThanks += SecondsOf(e));

        kk.AddCommandTransition(
            stateFrom:    ThanksForJoining,
            triggerCommandKey: "play again",
            replies: "KnowledgeKing is gonna start again!",
            does:    (_, ctx) => ResetGame(ctx),
            stateTo:      Questioning);

        // ── 外層:admin king-stop 回 Normal;ThanksForJoining 結束 20s → 回 Normal ──
        // (兩者目標都是外層 Normal → 必須宣告在外層。Thanks-20s 靠 ElapsedSecondsInThanks 當「在 Thanks」的代理條件;
        //  內層 ThanksForJoining 無對應 elapsed transition → 冒泡到外層。)
        bot.AddCommandTransition(stateFrom: KnowledgeKing, triggerCommandKey: "king-stop", adminOnly: true, stateTo: Normal);

        bot.AddTransition(
            stateFrom: KnowledgeKing, triggerEventName: BotEvents.Elapsed, stateTo: Normal,
            when: (_, ctx) => ctx.ElapsedSecondsInThanks >= ThanksTimeoutSeconds);
    }

    // ── 知識王:狀態進場 ──

    private static void OnEnterQuestioning(BotContext ctx)
    {
        ctx.Messenger.SendChat(ctx.QuizBank.QuestionAt(ctx.CurrentQuestionIndex));
        ctx.Messenger.SendChat("請 @bot 並回覆選項代號(A/B/C/D)作答");
        ctx.ElapsedSecondsInQuestion = 0;
        ctx.FirstCorrectAnswerer = null;
    }

    private static void OnEnterThanksForJoining(BotContext ctx)
    {
        ctx.ElapsedSecondsInThanks = 0;
        var result = BuildResult(ctx);
        if (ctx.SomeoneIsBroadcasting)
            ctx.Messenger.SendChat(result);
        else
            ctx.Messenger.Speak(result); // 無人廣播 → 語音公布
    }

    // ── 知識王:guard / action helper ──

    private static bool IsLastQuestion(BotContext ctx) =>
        ctx.CurrentQuestionIndex >= ctx.QuizBank.Count - 1;

    private static bool IsGameTimeout(BotContext ctx) =>
        ctx.ElapsedSecondsInGame >= GameTimeoutSeconds;

    private static bool IsFirstCorrectAnswer(Event e, BotContext ctx) =>
        e.Payload is ChatMessage m
        && m.TagsBot
        && ctx.FirstCorrectAnswerer is null
        && ctx.QuizBank.IsCorrect(ctx.CurrentQuestionIndex, m.Content);

    private static void AwardFirstCorrect(Event e, BotContext ctx)
    {
        var m = (ChatMessage)e.Payload!;
        ctx.FirstCorrectAnswerer = m.AuthorId;
        if (ctx.Users.TryGetValue(m.AuthorId, out var user))
            user.Score++;
        ctx.Messenger.SendChat("Congrats! you got the answer!", new[] { m.AuthorId });
    }

    private static void AccumulateElapsed(Event e, BotContext ctx)
    {
        var seconds = SecondsOf(e);
        ctx.ElapsedSecondsInQuestion += seconds;
        ctx.ElapsedSecondsInGame += seconds;
    }

    private static int SecondsOf(Event e) => e.Payload is int s ? s : 0;

    private static void ResetGame(BotContext ctx)
    {
        ctx.CurrentQuestionIndex = 0;
        ctx.ElapsedSecondsInGame = 0;
        ctx.ElapsedSecondsInQuestion = 0;
        ctx.ElapsedSecondsInThanks = 0; // 清掉上一場 Thanks 累計,避免新一場 Questioning 誤觸「Thanks 20s → Normal」
        foreach (var user in ctx.Users.Values)
            user.Score = 0;
    }

    // 結果:唯一最高分 → winner;多人同分 / 全 0 → Tie!。
    private static string BuildResult(BotContext ctx)
    {
        var top = ctx.Users.Values
            .OrderByDescending(u => u.Score)
            .ToList();
        if (top.Count == 0 || top[0].Score == 0)
            return "Tie!";
        if (top.Count > 1 && top[1].Score == top[0].Score)
            return "Tie!";
        return $"The winner is {top[0].Id}";
    }

    private static void DefineRecordState(BotBuilder<BotContext> bot)
    {
        var record = bot.AddCompositeState(Record,
            initialLeafStateResolver: ctx => ctx.SomeoneIsBroadcasting ? Recording : Waiting);

        record.AddLeafState(Waiting,
            onEnter: ctx => ctx.Messenger.SendChat("[Record] waiting for a broadcaster..."));

        // speak 用 onHandle 累積(不轉移):Handle 回 NotConsumed → 冒泡,外層無 speak transition → 靜默結束。
        record.AddLeafState(Recording,
            onEnter: ctx => ctx.Messenger.GoBroadcasting(),
            onHandle: AccumulateSpeak);

        record.AddTransition(stateFrom: Waiting, triggerEventName: BotEvents.GoBroadcasting, stateTo: Recording,
            does: (_, ctx) => ctx.SomeoneIsBroadcasting = true);

        // stop broadcasting:輸出 Record Replay + 清 buffer → 回 Waiting(循環)。
        record.AddTransition(stateFrom: Recording, triggerEventName: BotEvents.StopBroadcasting, stateTo: Waiting,
            does: EmitRecordReplay);

        // stop-recording(command,限錄音者):任意子狀態離開整個 Record 回 Normal。
        // 若在錄音中(buffer 有內容)→ 先輸出 Record Replay;等待中則無 Replay(沒錄到東西)。
        bot.AddTransition(stateFrom: Record, triggerEventName: BotEvents.NewMessage, stateTo: Normal,
            when: IsStopRecordingByRecorder,
            does: StopRecordingLeave);
    }

    // stop-recording 鐵律:tag bot + 內容 == "stop-recording" + 發話者 == 錄音者。
    private static bool IsStopRecordingByRecorder(Event e, BotContext ctx) =>
        e.Payload is ChatMessage m
        && m.TagsBot
        && m.Content == "stop-recording"
        && m.AuthorId == ctx.RecorderId;

    private static void StopRecordingLeave(Event e, BotContext ctx)
    {
        if (ctx.RecordBuffer.Count > 0) // 錄音中有累積 → 輸出 Replay(等待中 buffer 空 → 不輸出)
            EmitRecordReplay(e, ctx);
        ctx.SomeoneIsBroadcasting = false;
    }

    // ── 錄音:action / helper ──

    private static void StartRecording(Event e, BotContext ctx)
    {
        ctx.RecorderId = (e.Payload as ChatMessage)?.AuthorId; // 錄音者 = 下 record 指令者
        ctx.RecordBuffer.Clear();
    }

    private static void AccumulateSpeak(Event e, BotContext ctx)
    {
        if (e.Payload is Application.Parsing.SpeakInfo s)
            ctx.RecordBuffer.Add(s.Content);
    }

    // Record Replay:「[Record Replay] 」+ 各筆 speak 以換行分隔,結尾 @錄音者。
    private static void EmitRecordReplay(Event e, BotContext ctx)
    {
        var replay = "[Record Replay] " + string.Join("\n", ctx.RecordBuffer);
        var tags = ctx.RecorderId is null ? null : new[] { ctx.RecorderId };
        ctx.Messenger.SendChat(replay, tags);
        ctx.RecordBuffer.Clear();
    }
}
