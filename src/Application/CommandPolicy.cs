using Bot;
using Fsm.Core;

namespace Application;

internal static class CommandPolicy
{
    public static bool Is(Event e, BotContext ctx, string keyword) =>
        e.Payload is ChatMessage m && m.TagsBot && m.Content == keyword;
}
