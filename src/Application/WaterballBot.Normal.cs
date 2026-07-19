using Bot;
using static Application.BotFeatures;

namespace Application;

public static partial class WaterballBot
{
    private const int InteractingThreshold = 10;

    private static void DefineNormalState(BotBuilder<BotContext> bot)
    {
        var normal = bot.AddCompositeState(Normal,
            initialLeafStateResolver: ctx => ctx.OnlineCount < InteractingThreshold ? Default : Interacting);
        normal.AddLeafState(Default,
            botAutoRotateMessage: ["good to hear", "thank you", "How are you"]);
        normal.AddLeafState(Interacting,
            botAutoRotateMessage: ["nice to see you", "welcome back", "let's chat"]);

        bot.AddTransition(Normal, BotEvents.NewMessage, KnowledgeKing,
            When(CommandPolicy.Is("king")),
            When(AdminPolicy.IsAdmin),
            When(CostPolicy.HasQuota(5)),
            Do(CostPolicy.DeductQuota(5)),
            Do(ReplyPolicy.SendChat("KnowledgeKing is started!")),
            Do(KnowledgeKingPolicy.ResetGame));

        bot.AddTransition(Normal, BotEvents.NewMessage, Record,
            When(CommandPolicy.Is("record")),
            When(CostPolicy.HasQuota(3)),
            Do(CostPolicy.DeductQuota(3)),
            Do(RecordingPolicy.StartRecording));

        bot.AddTransition(Normal, BotEvents.Login, Normal,
            Do((_, ctx) => ctx.OnlineCount++));
        bot.AddTransition(Normal, BotEvents.Logout, Normal,
            Do((_, ctx) => ctx.OnlineCount = Math.Max(0, ctx.OnlineCount - 1)));
    }
}
