using Bot;
using static Application.BotFeatures;

namespace Application;

public static partial class WaterballBot
{
    private static void DefineKnowledgeKingState(BotBuilder<BotContext> bot)
    {
        var kk = bot.AddCompositeState(KnowledgeKing);

        kk.AddLeafState(Questioning,
            onEnter: KnowledgeKingPolicy.OnEnterQuestioning,
            onHandle: KnowledgeKingPolicy.AccumulateElapsed);

        kk.AddTransition(Questioning, BotEvents.Elapsed, ThanksForJoining,
            When(KnowledgeKingPolicy.IsGameTimeout));

        kk.AddTransition(Questioning, BotEvents.NewMessage, Questioning,
            When((e, ctx) => KnowledgeKingPolicy.IsFirstCorrectAnswer(e, ctx) && !KnowledgeKingPolicy.IsLastQuestion(ctx)),
            Do((e, ctx) => { KnowledgeKingPolicy.AwardToFirstCorrectPlayer(e, ctx); ctx.CurrentQuestionIndex++; }));

        kk.AddTransition(Questioning, BotEvents.NewMessage, ThanksForJoining,
            When((e, ctx) => KnowledgeKingPolicy.IsFirstCorrectAnswer(e, ctx) && KnowledgeKingPolicy.IsLastQuestion(ctx)),
            Do(KnowledgeKingPolicy.AwardToFirstCorrectPlayer));

        kk.AddTransition(Questioning, BotEvents.Elapsed, Questioning,
            When((_, ctx) => ctx.ElapsedSecondsInQuestion >= KnowledgeKingPolicy.QuestionTimeoutSeconds && !KnowledgeKingPolicy.IsLastQuestion(ctx)),
            Do((_, ctx) => ctx.CurrentQuestionIndex++));

        kk.AddTransition(Questioning, BotEvents.Elapsed, ThanksForJoining,
            When((_, ctx) => ctx.ElapsedSecondsInQuestion >= KnowledgeKingPolicy.QuestionTimeoutSeconds && KnowledgeKingPolicy.IsLastQuestion(ctx)));


        kk.AddLeafState(ThanksForJoining,
            onEnter: KnowledgeKingPolicy.OnEnterThanksForJoining,
            onHandle: (e, ctx) => ctx.ElapsedSecondsInThanks += KnowledgeKingPolicy.GetElapsedSeconds(e));

        kk.AddTransition(ThanksForJoining, BotEvents.NewMessage, Questioning,
            When(CommandPolicy.Is("play again")),
            Do(ReplyPolicy.SendChat("KnowledgeKing is gonna start again!")),
            Do(KnowledgeKingPolicy.ResetGame));

        bot.AddTransition(KnowledgeKing, BotEvents.NewMessage, Normal,
            When(CommandPolicy.Is("king-stop")), When(AdminPolicy.IsAdmin));

        bot.AddTransition(KnowledgeKing, BotEvents.Elapsed, Normal,
            When((_, ctx) => ctx.ElapsedSecondsInThanks >= KnowledgeKingPolicy.ThanksTimeoutSeconds));
    }
}
