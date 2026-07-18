using Application;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

// Command / Admin / Cost 的行為已由 WaterballBotTests 端到端覆蓋(邏輯現位於 Application 的
// CommandPolicy / AdminPolicy / CostPolicy)。此處只針對可重用的 GuardList 組合語意。
public class GuardListTests
{
    private static Event Msg(string content, bool tagsBot) =>
        new(BotEvents.NewMessage, new ChatMessage("1", content, tagsBot));

    private static IGuard<BotContext> TagsBot(string keyword) =>
        new PredicateGuard<BotContext>((e, _) =>
            e.Payload is ChatMessage m && m.TagsBot && m.Content == keyword);

    private static IGuard<BotContext> HasQuota(int amount) =>
        new PredicateGuard<BotContext>((_, ctx) => ctx.TokenQuota >= amount);

    [Fact]
    public void GuardList_requires_all_to_pass()
    {
        var ctx = new BotContext(new SpyMessenger(), initialTokenQuota: 5);
        var e = Msg("king", tagsBot: true);

        Assert.True(new GuardList<BotContext>(TagsBot("king"), HasQuota(5)).Test(e, ctx));

        // 缺額度 → 整體 false
        Assert.False(new GuardList<BotContext>(TagsBot("king"), HasQuota(6)).Test(e, ctx));
    }

    [Fact]
    public void GuardList_empty_is_always_true()
    {
        var ctx = new BotContext(new SpyMessenger(), initialTokenQuota: 0);
        Assert.True(new GuardList<BotContext>().Test(Msg("x", tagsBot: false), ctx));
    }
}
