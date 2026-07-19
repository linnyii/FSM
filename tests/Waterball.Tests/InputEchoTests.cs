using Application.Events;
using Xunit;

namespace Waterball.Tests;

public class InputEchoTests
{
    private static string Echo(IDomainEvent e)
    {
        var sw = new StringWriter();
        e.ConsoleOutput(sw);
        return sw.ToString().TrimEnd('\r', '\n');
    }

    [Fact]
    public void Chat_event_echoes_with_tags()
    {
        Assert.Equal("💬 3: 哈哈 @1, @2, @4", Echo(new ChatEvent("3", "哈哈", new[] { "1", "2", "4" })));
    }

    [Fact]
    public void Chat_event_echoes_without_tags()
    {
        Assert.Equal("💬 1: 早安", Echo(new ChatEvent("1", "早安", [])));
    }

    [Fact]
    public void Post_event_echoes()
    {
        Assert.Equal("4:【標題】內文 @1, @2", Echo(new PostEvent("1", "4", "標題", "內文", new[] { "1", "2" })));
    }

    [Fact]
    public void Go_broadcasting_echoes()
    {
        Assert.Equal("📢 4 is broadcasting...", Echo(new GoBroadcastingEvent("4")));
    }

    [Fact]
    public void Speak_echoes()
    {
        Assert.Equal("📢 4: 大家早安", Echo(new SpeakEvent("4", "大家早安")));
    }

    [Fact]
    public void Stop_broadcasting_echoes()
    {
        Assert.Equal("📢 4 stop broadcasting", Echo(new StopBroadcastingEvent("4")));
    }

    [Fact]
    public void Elapsed_echoes_raw_shell_not_converted_seconds()
    {
        // payload seconds = 3600,但回顯用原始外殼 "1 hours"。
        Assert.Equal("🕑 1 hours elapsed...", Echo(new ElapsedEvent(3600, "1 hours")));
    }

    [Fact]
    public void Login_logout_started_end_do_not_echo()
    {
        Assert.Equal("", Echo(new LoginEvent("1", true)));
        Assert.Equal("", Echo(new LogoutEvent("1")));
        Assert.Equal("", Echo(new StartedEvent("t", 10)));
        Assert.Equal("", Echo(new EndEvent()));
    }
}
