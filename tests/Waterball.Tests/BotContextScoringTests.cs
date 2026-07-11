using Application;
using Xunit;

namespace Waterball.Tests;

public class BotContextScoringTests
{
    private static BotContext New() => new(new SpyMessenger(), initialTokenQuota: 100);

    [Fact]
    public void New_context_has_zeroed_elapsed_counters()
    {
        var ctx = New();
        Assert.Equal(0, ctx.ElapsedSecondsInQuestion);
        Assert.Equal(0, ctx.ElapsedSecondsInGame);
        Assert.Equal(0, ctx.ElapsedSecondsInThanks);
    }

    [Fact]
    public void New_context_has_no_first_correct_answerer()
    {
        Assert.Null(New().FirstCorrectAnswerer);
    }

    [Fact]
    public void UpsertUser_starts_with_zero_score_and_marks_online()
    {
        var ctx = New();
        var user = ctx.UpsertUser("7", isAdmin: false);
        Assert.Equal(0, user.Score);
        Assert.True(user.IsOnline);
        Assert.Same(user, ctx.Users["7"]);
    }

    [Fact]
    public void SeedQuota_replaces_token_quota()
    {
        var ctx = New();
        ctx.SeedQuota(10);
        Assert.Equal(10, ctx.TokenQuota);
    }
}
