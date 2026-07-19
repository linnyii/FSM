using Application.Parsing;
using Bot;
using Fsm.Core;

namespace Application.Events;

public sealed record GoBroadcastingEvent(string SpeakerId) : IDomainEvent
{
    public void ConsoleOutput(TextWriter output) => output.WriteLine($"📢 {SpeakerId} is broadcasting...");
    public Event ToFsmEvent() => new(BotEvents.GoBroadcasting, new BroadcastInfo(SpeakerId));
}

public sealed record SpeakEvent(string SpeakerId, string Content) : IDomainEvent
{
    public void ConsoleOutput(TextWriter output) => output.WriteLine($"📢 {SpeakerId}: {Content}");
    public Event ToFsmEvent() => new(BotEvents.Speak, new SpeakInfo(SpeakerId, Content));
}

public sealed record StopBroadcastingEvent(string SpeakerId) : IDomainEvent
{
    public void ConsoleOutput(TextWriter output) => output.WriteLine($"📢 {SpeakerId} stop broadcasting");
    public Event ToFsmEvent() => new(BotEvents.StopBroadcasting, new BroadcastInfo(SpeakerId));
}
