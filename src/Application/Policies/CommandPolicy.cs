using Bot;
using Fsm.Core;

namespace Application;

internal static class CommandPolicy
{
    public static Func<Event, BotContext, bool> Is(string keyword) =>
        (e, _) => e.Payload is ChatMessage m && m.TagsBot && m.Content == keyword;
}
