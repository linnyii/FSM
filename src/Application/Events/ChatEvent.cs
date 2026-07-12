using Bot;
using Fsm.Core;

namespace Application.Events;

public sealed record ChatEvent(string AuthorId, string Content, IReadOnlyList<string> Tags) : IDomainEvent
{
    private bool TagsBot => Tags.Contains("bot");

    public void Echo(TextWriter output) => output.WriteLine($"💬 {AuthorId}: {Content}{Output.Tags.Format(Tags)}");

    public void ApplyCustomizedEventInfoTo(BotContext ctx) => ctx.SetCurrentUser(AuthorId);

    public Event ToFsmEvent() =>
        new(BotEvents.NewMessage, new ChatMessage(AuthorId, Content, TagsBot, Tags));
}
