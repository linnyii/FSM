using Application;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class NamedGuardActionTests
{
    private static (Event e, BotContext ctx) Setup(string content, bool tagsBot, bool admin, int quota)
    {
        var ctx = new BotContext(new SpyMessenger(), initialTokenQuota: quota)
        {
            CurrentUser = new User("1", isAdmin: admin),
        };
        var e = new Event(BotEvents.NewMessage, new ChatMessage("1", content, tagsBot));
        return (e, ctx);
    }

    [Fact]
    public void CommandIsGuard_true_only_when_tags_bot_and_content_matches()
    {
        var (e, ctx) = Setup("king", tagsBot: true, admin: false, quota: 0);
        Assert.True(new CommandIsGuard<BotContext>("king").Test(e, ctx));

        var (e2, ctx2) = Setup("king", tagsBot: false, admin: false, quota: 0);
        Assert.False(new CommandIsGuard<BotContext>("king").Test(e2, ctx2));

        var (e3, ctx3) = Setup("record", tagsBot: true, admin: false, quota: 0);
        Assert.False(new CommandIsGuard<BotContext>("king").Test(e3, ctx3));
    }

    [Fact]
    public void IsAdminGuard_reflects_current_user()
    {
        var (e, ctx) = Setup("x", tagsBot: true, admin: true, quota: 0);
        Assert.True(new IsAdminGuard<BotContext>().Test(e, ctx));

        ctx.CurrentUser = new User("2", isAdmin: false);
        Assert.False(new IsAdminGuard<BotContext>().Test(e, ctx));

        ctx.CurrentUser = null;
        Assert.False(new IsAdminGuard<BotContext>().Test(e, ctx));
    }

    [Fact]
    public void HasQuotaGuard_checks_threshold()
    {
        var (e, ctx) = Setup("x", tagsBot: true, admin: false, quota: 5);
        Assert.True(new HasQuotaGuard<BotContext>(5).Test(e, ctx));
        Assert.False(new HasQuotaGuard<BotContext>(6).Test(e, ctx));
    }

    [Fact]
    public void AndGuard_requires_all_to_pass()
    {
        var (e, ctx) = Setup("king", tagsBot: true, admin: true, quota: 5);
        var all = new AndGuard<BotContext>(
            new CommandIsGuard<BotContext>("king"),
            new IsAdminGuard<BotContext>(),
            new HasQuotaGuard<BotContext>(5));
        Assert.True(all.Test(e, ctx));

        // 缺額度 → 整體 false
        var lacking = new AndGuard<BotContext>(
            new CommandIsGuard<BotContext>("king"),
            new HasQuotaGuard<BotContext>(6));
        Assert.False(lacking.Test(e, ctx));
    }

    [Fact]
    public void AndGuard_empty_is_always_true()
    {
        var (e, ctx) = Setup("x", tagsBot: false, admin: false, quota: 0);
        Assert.True(new AndGuard<BotContext>().Test(e, ctx));
    }

    [Fact]
    public void DeductQuotaAction_and_SendChatAction_execute()
    {
        var spy = new SpyMessenger();
        var ctx = new BotContext(spy, initialTokenQuota: 10);
        var e = new Event("x");

        new DeductQuotaAction<BotContext>(3).Execute(e, ctx);
        Assert.Equal(7, ctx.TokenQuota);

        new SendChatAction<BotContext>("hello").Execute(e, ctx);
        Assert.Contains("chat:hello", spy.Log);
    }
}
