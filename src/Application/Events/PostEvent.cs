using Application.Parsing;
using Bot;
using Fsm.Core;

namespace Application.Events;

public sealed record PostEvent(string Id, string AuthorId, string Title, string Content, IReadOnlyList<string> Tags)
    : IDomainEvent
{
    public void ConsoleOutput(TextWriter output) => output.WriteLine($"{AuthorId}:【{Title}】{Content}{Output.Tags.Format(Tags)}");

    public void ApplyCustomizedEventInfoTo(BotContext ctx) => ctx.SetCurrentUser(AuthorId);

    public Event ToFsmEvent() =>
        new(BotEvents.NewPost, new NewPost(Id, AuthorId, Title, Content, Tags));
}
