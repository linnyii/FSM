using Application.Output;
using Application.Parsing;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class InputEchoTests
{
    private static string Echo(Event e, string rawLine)
    {
        var sw = new StringWriter();
        new InputEcho(sw).Echo(e, rawLine);
        return sw.ToString().TrimEnd('\r', '\n');
    }

    [Fact]
    public void Chat_message_with_tags()
    {
        var e = new Event(BotEvents.NewMessage, new ChatMessage("3", "哈哈", tagsBot: false, tags: new[] { "1", "2", "4" }));
        Assert.Equal("💬 3: 哈哈 @1, @2, @4", Echo(e, "[new message] {}"));
    }

    [Fact]
    public void Chat_message_without_tags()
    {
        var e = new Event(BotEvents.NewMessage, new ChatMessage("1", "早安", tagsBot: false));
        Assert.Equal("💬 1: 早安", Echo(e, "[new message] {}"));
    }

    [Fact]
    public void New_post()
    {
        var e = new Event(BotEvents.NewPost, new NewPost("1", "4", "標題", "內文", new[] { "1", "2" }));
        Assert.Equal("4:【標題】內文 @1, @2", Echo(e, "[new post] {}"));
    }

    [Fact]
    public void Go_broadcasting()
    {
        var e = new Event(BotEvents.GoBroadcasting, new BroadcastInfo("4"));
        Assert.Equal("📢 4 is broadcasting...", Echo(e, "[go broadcasting] {}"));
    }

    [Fact]
    public void Speak()
    {
        var e = new Event(BotEvents.Speak, new SpeakInfo("4", "大家早安"));
        Assert.Equal("📢 4: 大家早安", Echo(e, "[speak] {}"));
    }

    [Fact]
    public void Stop_broadcasting()
    {
        var e = new Event(BotEvents.StopBroadcasting, new BroadcastInfo("4"));
        Assert.Equal("📢 4 stop broadcasting", Echo(e, "[stop broadcasting] {}"));
    }

    [Fact]
    public void Elapsed_uses_raw_shell_not_converted_seconds()
    {
        var e = new Event(BotEvents.Elapsed, 3600); // payload 是換算後 seconds
        Assert.Equal("🕑 1 hours elapsed...", Echo(e, "[1 hours elapsed]")); // 回顯用原始外殼
    }

    [Fact]
    public void Login_and_logout_are_not_echoed()
    {
        Assert.Equal("", Echo(new Event(BotEvents.Login, new LoginInfo("1", true)), "[login] {}"));
        Assert.Equal("", Echo(new Event(BotEvents.Logout, new LogoutInfo("1")), "[logout] {}"));
    }
}
