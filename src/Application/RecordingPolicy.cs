using Bot;
using Fsm.Core;

namespace Application;

internal static class RecordingPolicy
{
    public static bool IsStopRecordingByCurrentRecorder(Event e, BotContext ctx) =>
        e.Payload is ChatMessage { TagsBot: true } m
        && m.Content == "stop-recording"
        && m.AuthorId == ctx.RecorderId;

    public static void StartRecording(Event e, BotContext ctx)
    {
        ctx.RecorderId = (e.Payload as ChatMessage)?.AuthorId; // 錄音者 = 下 record 指令者
        ctx.RecordBuffer.Clear();
    }

    public static void AccumulateSpeak(Event e, BotContext ctx)
    {
        if (e.Payload is Parsing.SpeakInfo s)
            ctx.RecordBuffer.Add(s.Content);
    }

    public static void RecordReplayForStopRecordingLeave(Event e, BotContext ctx)
    {
        if (ctx.RecordBuffer.Count > 0) 
            RecordReplay(e, ctx);
        ctx.SomeoneIsBroadcasting = false;
    }

    public static void RecordReplay(Event e, BotContext ctx)
    {
        var replay = "[Record Replay] " + string.Join("\n", ctx.RecordBuffer);
        var tags = ctx.RecorderId is null ? null : new[] { ctx.RecorderId };
        ctx.Messenger.SendChat(replay, tags);
        ctx.RecordBuffer.Clear();
    }
}
