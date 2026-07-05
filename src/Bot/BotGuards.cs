using Fsm.Core;

namespace Bot;

/// <summary>bot module 內建、可重用的 guard（橫切關注：權限、額度、指令匹配）。</summary>
internal static class BotGuards
{
    /// <summary>指令鐵律：訊息 tag 到 bot 且內容 == 關鍵字。</summary>
    public static IGuard<C> CommandIs<C>(string keyword) =>
        new PredicateGuard<C>((e, _) =>
            e.Payload is ChatMessage m && m.TagsBot && m.Content == keyword);

    /// <summary>權限橫切：發訊息的人是管理員。</summary>
    public static IGuard<C> IsAdmin<C>() where C : IBotContext =>
        new PredicateGuard<C>((_, ctx) => ctx.IsCurrentUserAdmin);

    /// <summary>額度橫切（檢查）：夠不夠 n。</summary>
    public static IGuard<C> HasQuota<C>(int amount) where C : IBotContext =>
        new PredicateGuard<C>((_, ctx) => ctx.TokenQuota >= amount);
}
