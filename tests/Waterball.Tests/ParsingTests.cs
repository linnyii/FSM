using Application.Parsing;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class ParsingTests
{
    // ── 每個小 parser:給 json → 斷言 Event(name + payload record) ──

    [Fact]
    public void NewMessageParser_produces_ChatMessage_with_TagsBot_when_tags_contain_bot()
    {
        var e = new NewMessageParser().Parse("{\"authorId\":\"5\",\"content\":\"hi\",\"tags\":[\"1\",\"3\",\"bot\"]}");

        Assert.Equal(BotEvents.NewMessage, e.Name);
        var m = Assert.IsType<ChatMessage>(e.Payload);
        Assert.Equal("5", m.AuthorId);
        Assert.Equal("hi", m.Content);
        Assert.True(m.TagsBot);
    }

    [Fact]
    public void NewMessageParser_TagsBot_false_when_no_bot_tag()
    {
        var e = new NewMessageParser().Parse("{\"authorId\":\"5\",\"content\":\"hi\",\"tags\":[\"1\"]}");
        Assert.False(Assert.IsType<ChatMessage>(e.Payload).TagsBot);
    }

    [Fact]
    public void NewPostParser_produces_NewPost()
    {
        var e = new NewPostParser().Parse(
            "{\"id\":\"1\",\"authorId\":\"8\",\"title\":\"T\",\"content\":\"C\",\"tags\":[\"1\",\"2\"]}");

        Assert.Equal(BotEvents.NewPost, e.Name);
        var p = Assert.IsType<NewPost>(e.Payload);
        Assert.Equal("1", p.Id);
        Assert.Equal("8", p.AuthorId);
        Assert.Equal("T", p.Title);
        Assert.Equal(new[] { "1", "2" }, p.Tags);
    }

    [Fact]
    public void GoBroadcastingParser_produces_BroadcastInfo()
    {
        var e = new GoBroadcastingParser().Parse("{\"speakerId\":\"4\"}");
        Assert.Equal(BotEvents.GoBroadcasting, e.Name);
        Assert.Equal("4", Assert.IsType<BroadcastInfo>(e.Payload).SpeakerId);
    }

    [Fact]
    public void SpeakParser_produces_SpeakInfo()
    {
        var e = new SpeakParser().Parse("{\"speakerId\":\"4\",\"content\":\"大家早安\"}");
        Assert.Equal(BotEvents.Speak, e.Name);
        var s = Assert.IsType<SpeakInfo>(e.Payload);
        Assert.Equal("4", s.SpeakerId);
        Assert.Equal("大家早安", s.Content);
    }

    [Fact]
    public void StopBroadcastingParser_produces_BroadcastInfo()
    {
        var e = new StopBroadcastingParser().Parse("{\"speakerId\":\"4\"}");
        Assert.Equal(BotEvents.StopBroadcasting, e.Name);
        Assert.Equal("4", Assert.IsType<BroadcastInfo>(e.Payload).SpeakerId);
    }

    [Fact]
    public void LoginParser_produces_LoginInfo_with_isAdmin()
    {
        var e = new LoginParser().Parse("{\"userId\":\"1\",\"isAdmin\":true}");
        Assert.Equal(BotEvents.Login, e.Name);
        var l = Assert.IsType<LoginInfo>(e.Payload);
        Assert.Equal("1", l.UserId);
        Assert.True(l.IsAdmin);
    }

    [Fact]
    public void LogoutParser_produces_LogoutInfo()
    {
        var e = new LogoutParser().Parse("{\"userId\":\"1\"}");
        Assert.Equal(BotEvents.Logout, e.Name);
        Assert.Equal("1", Assert.IsType<LogoutInfo>(e.Payload).UserId);
    }

    [Fact]
    public void StartedParser_produces_StartedInfo_with_quota()
    {
        var e = new StartedParser().Parse("{\"time\":\"2023-08-07 00:00:00\",\"quota\":10}");
        Assert.Equal(BotEvents.Started, e.Name);
        Assert.Equal(10, Assert.IsType<StartedInfo>(e.Payload).Quota);
    }

    [Fact]
    public void EndParser_produces_end_event_no_payload()
    {
        var e = new EndParser().Parse("");
        Assert.Equal(BotEvents.End, e.Name);
        Assert.Null(e.Payload);
    }

    [Theory]
    [InlineData("10 seconds", 10)]
    [InlineData("2 minutes", 120)]
    [InlineData("1 hours", 3600)]
    public void ElapsedParser_converts_unit_to_seconds(string shell, int expected)
    {
        var e = new ElapsedParser().Parse(shell);
        Assert.Equal(BotEvents.Elapsed, e.Name);
        Assert.Equal(expected, Assert.IsType<int>(e.Payload));
    }

    // ── 每個 parser 的 Name 對應 [name] ──

    [Fact]
    public void Parser_names_match_event_names()
    {
        Assert.Equal("new message", new NewMessageParser().Name);
        Assert.Equal("new post", new NewPostParser().Name);
        Assert.Equal("go broadcasting", new GoBroadcastingParser().Name);
        Assert.Equal("speak", new SpeakParser().Name);
        Assert.Equal("stop broadcasting", new StopBroadcastingParser().Name);
        Assert.Equal("login", new LoginParser().Name);
        Assert.Equal("logout", new LogoutParser().Name);
        Assert.Equal("elapsed", new ElapsedParser().Name);
        Assert.Equal("started", new StartedParser().Name);
        Assert.Equal("end", new EndParser().Name);
    }

    // ── 分派器(註冊表) ──

    private static EventParser Dispatcher() => new(new IEventParser[]
    {
        new NewMessageParser(), new NewPostParser(), new GoBroadcastingParser(),
        new SpeakParser(), new StopBroadcastingParser(), new LoginParser(),
        new LogoutParser(), new ElapsedParser(), new StartedParser(), new EndParser(),
    });

    [Fact]
    public void Dispatcher_routes_login_shell_to_LoginParser()
    {
        var e = Dispatcher().Parse("[login] {\"userId\":\"1\",\"isAdmin\":true}");
        Assert.NotNull(e);
        Assert.Equal(BotEvents.Login, e!.Name);
        Assert.IsType<LoginInfo>(e.Payload);
    }

    [Fact]
    public void Dispatcher_routes_elapsed_shell_and_converts_seconds()
    {
        var e = Dispatcher().Parse("[10 seconds elapsed]");
        Assert.NotNull(e);
        Assert.Equal(BotEvents.Elapsed, e!.Name);
        Assert.Equal(10, e.Payload);
    }

    [Fact]
    public void Dispatcher_returns_null_for_blank_line()
    {
        Assert.Null(Dispatcher().Parse("   "));
    }

    [Fact]
    public void Dispatcher_throws_on_unknown_name()
    {
        var ex = Assert.Throws<UnknownEventException>(() => Dispatcher().Parse("[nonsense] {}"));
        Assert.Equal("nonsense", ex.EventName);
    }

    [Fact]
    public void Dispatcher_uses_injected_parsers_by_name_without_hardcoding()
    {
        // 塞一組假 parser → 驗證「加新 parser 免改分派器」:分派器純靠 Name 建表查表。
        var fake = new FakeParser("custom", new Event("custom", "PAYLOAD"));
        var dispatcher = new EventParser(new IEventParser[] { fake });

        var e = dispatcher.Parse("[custom] {}");

        Assert.Equal("custom", e!.Name);
        Assert.Equal("PAYLOAD", e.Payload);
    }

    private sealed class FakeParser(string name, Event result) : IEventParser
    {
        public string Name => name;
        public Event Parse(string json) => result;
    }
}
