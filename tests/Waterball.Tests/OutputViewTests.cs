using Application.Output;
using Xunit;

namespace Waterball.Tests;

public class OutputViewTests
{
    private static string Capture(Action<StringWriter> act)
    {
        var sw = new StringWriter();
        act(sw);
        return sw.ToString().TrimEnd('\r', '\n');
    }

    [Fact]
    public void ChatRoomView_bot_says_with_tags()
    {
        var line = Capture(sw => new ChatRoomView(sw).BotSays("thank you", new[] { "3", "4" }));
        Assert.Equal("🤖: thank you @3, @4", line);
    }

    [Fact]
    public void ChatRoomView_bot_says_without_tags()
    {
        var line = Capture(sw => new ChatRoomView(sw).BotSays("good to hear"));
        Assert.Equal("🤖: good to hear", line);
    }

    [Fact]
    public void ForumView_bot_comments_in_post()
    {
        var line = Capture(sw => new ForumView(sw).BotComments("1", "Nice post", new[] { "2" }));
        Assert.Equal("🤖 comment in post 1: Nice post @2", line);
    }

    [Fact]
    public void BroadcastView_start_speak_stop()
    {
        Assert.Equal("🤖 go broadcasting...", Capture(sw => new BroadcastView(sw).BotStarts()));
        Assert.Equal("🤖 speaking: The winner is 2", Capture(sw => new BroadcastView(sw).BotSpeaks("The winner is 2")));
        Assert.Equal("🤖 stop broadcasting...", Capture(sw => new BroadcastView(sw).BotStops()));
    }

    [Fact]
    public void Tags_use_at_prefix_comma_space_separator_including_bot()
    {
        var line = Capture(sw => new ChatRoomView(sw).BotSays("hi", new[] { "3", "4", "bot" }));
        Assert.Equal("🤖: hi @3, @4, @bot", line);
    }
}
