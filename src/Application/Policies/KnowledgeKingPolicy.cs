using Bot;
using Fsm.Core;

namespace Application;

internal static class KnowledgeKingPolicy
{
    public const int QuestionTimeoutSeconds = 20;
    public const int GameTimeoutSeconds = 3600;
    public const int ThanksTimeoutSeconds = 20;


    public static void OnEnterQuestioning(BotContext ctx)
    {
        ctx.Messenger.SendChat(ctx.QuizBank.GetTheQuestionAt(ctx.CurrentQuestionIndex));
        ctx.Messenger.SendChat("請 @bot 並回覆選項代號(A/B/C/D)作答");
        ctx.ElapsedSecondsInQuestion = 0;
        ctx.FirstCorrectAnswerer = null;
    }

    public static void OnEnterThanksForJoining(BotContext ctx)
    {
        ctx.ElapsedSecondsInThanks = 0;
        var result = BuildGameResultMsg(ctx);
        if (ctx.SomeoneIsBroadcasting)
            ctx.Messenger.SendChat(result);
        else
            ctx.Messenger.Speak(result); // 無人廣播 → 語音公布
    }


    public static bool IsLastQuestion(BotContext ctx) =>
        ctx.CurrentQuestionIndex >= ctx.QuizBank.Count - 1;

    public static bool IsGameTimeout(Event _, BotContext ctx) =>
        ctx.ElapsedSecondsInGame >= GameTimeoutSeconds;

    public static bool IsFirstCorrectAnswer(Event e, BotContext ctx) =>
        e.Payload is ChatMessage m
        && m.TagsBot
        && ctx.FirstCorrectAnswerer is null
        && ctx.QuizBank.CheckIsCorrect(ctx.CurrentQuestionIndex, m.Content);

    public static void AwardToFirstCorrectPlayer(Event e, BotContext ctx)
    {
        var m = (ChatMessage)e.Payload!;
        ctx.FirstCorrectAnswerer = m.AuthorId;
        if (ctx.Users.TryGetValue(m.AuthorId, out var user))
            user.Score++;
        ctx.Messenger.SendChat("Congrats! you got the answer!", new[] { m.AuthorId });
    }

    public static void AccumulateElapsed(Event e, BotContext ctx)
    {
        var seconds = GetElapsedSeconds(e);
        ctx.ElapsedSecondsInQuestion += seconds;
        ctx.ElapsedSecondsInGame += seconds;
    }

    public static int GetElapsedSeconds(Event e) => e.Payload is int s ? s : 0;

    public static void ResetGame(Event _, BotContext ctx)
    {
        ctx.CurrentQuestionIndex = 0;
        ctx.ElapsedSecondsInGame = 0;
        ctx.ElapsedSecondsInQuestion = 0;
        ctx.ElapsedSecondsInThanks = 0;
        foreach (var user in ctx.Users.Values)
            user.Score = 0;
    }

    private static string BuildGameResultMsg(BotContext ctx)
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
}
