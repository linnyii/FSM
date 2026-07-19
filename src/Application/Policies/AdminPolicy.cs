using Bot;
using Fsm.Core;

namespace Application;

internal static class AdminPolicy
{
    public static bool IsAdmin(Event _, BotContext ctx) => ctx.CurrentUser?.IsAdmin == true;
}
