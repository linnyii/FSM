using Bot;

namespace Application;

internal static class AdminPolicy
{
    public static bool IsAdmin(BotContext ctx) => ctx.CurrentUser?.IsAdmin == true;
}
