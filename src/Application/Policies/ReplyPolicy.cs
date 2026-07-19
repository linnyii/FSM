using Bot;
using Fsm.Core;

namespace Application;

internal static class ReplyPolicy
{
    public static Action<Event, BotContext> SendChat(string content) =>
        (_, ctx) => ctx.Messenger.SendChat(content);
}
