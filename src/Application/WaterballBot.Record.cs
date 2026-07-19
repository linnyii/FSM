using Bot;
using static Application.BotFeatures;

namespace Application;

public static partial class WaterballBot
{
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
