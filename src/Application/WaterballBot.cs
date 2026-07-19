using Bot;
using Fsm.Core;

namespace Application;

public static partial class WaterballBot
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

    public static FiniteStateMachine<BotContext> Define()
    {
        var bot = new BotBuilder<BotContext>();

        DefineNormalState(bot);
        DefineKnowledgeKingState(bot);
        DefineRecordState(bot);

        bot.InitStateFrom(Normal);
        return bot.Build();
    }
}
