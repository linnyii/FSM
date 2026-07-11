using Fsm.Core;

namespace Bot;

/// <summary>指令鐵律:訊息 tag 到 bot 且內容 == 關鍵字。</summary>
public sealed class CommandIsGuard<C>(string keyword) : IGuard<C>
{
    public bool Test(Event @event, C ctx) =>
        @event.Payload is ChatMessage m && m.TagsBot && m.Content == keyword;
}

/// <summary>權限橫切:當前發話者是管理員。</summary>
public sealed class IsAdminGuard<C> : IGuard<C> where C : IBotContext
{
    public bool Test(Event @event, C ctx) => ctx.CurrentUser?.IsAdmin == true;
}

/// <summary>額度橫切(檢查):共享額度夠不夠 n。</summary>
public sealed class HasQuotaGuard<C>(int amount) : IGuard<C> where C : IBotContext
{
    public bool Test(Event @event, C ctx) => ctx.TokenQuota >= amount;
}

/// <summary>bot module 內建、可重用的 guard 工廠(回具名 class,供 BotBuilder 收集組成 AndGuard)。</summary>
internal static class BotGuards
{
    public static IGuard<C> CommandIs<C>(string keyword) => new CommandIsGuard<C>(keyword);

    public static IGuard<C> IsAdmin<C>() where C : IBotContext => new IsAdminGuard<C>();

    public static IGuard<C> HasQuota<C>(int amount) where C : IBotContext => new HasQuotaGuard<C>(amount);
}
