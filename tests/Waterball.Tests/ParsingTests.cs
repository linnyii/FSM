using Application.Events;
using Application.Parsing;
using Bot;
using Fsm.Core;
using Xunit;

namespace Waterball.Tests;

public class ParsingTests
{
    // ── 每個小 parser:給 json → 斷言產出正確的 IDomainEvent 具體型別/欄位 ──

    [Fact]
    public void NewMessageParser_produces_ChatEvent_with_tags()
    {
        var e = Assert.IsType<ChatEvent>(
            new NewMessageParser().Parse("{\"authorId\":\"5\",\"content\":\"hi\",\"tags\":[\"1\",\"3\",\"bot\"]}"));
        Assert.Equal("5", e.AuthorId);
        Assert.Equal("hi", e.Content);
        Assert.Equal(new[] { "1", "3", "bot" }, e.Tags);

        // ToFsmEvent 的 payload 帶 TagsBot（tags 含 "bot"）。
        var fsm = e.ToFsmEvent();
        Assert.Equal(BotEvents.NewMessage, fsm.Name);
        Assert.True(Assert.IsType<ChatMessage>(fsm.Payload).TagsBot);
    }

    [Fact]
    public void NewMessageParser_TagsBot_false_when_no_bot_tag()
    {
        var e = new NewMessageParser().Parse("{\"authorId\":\"5\",\"content\":\"hi\",\"tags\":[\"1\"]}");
        Assert.False(Assert.IsType<ChatMessage>(e.ToFsmEvent().Payload).TagsBot);
    }

    [Fact]
    public void NewPostParser_produces_PostEvent()
    {
        var e = Assert.IsType<PostEvent>(new NewPostParser().Parse(
            "{\"id\":\"1\",\"authorId\":\"8\",\"title\":\"T\",\"content\":\"C\",\"tags\":[\"1\",\"2\"]}"));
        Assert.Equal("1", e.Id);
        Assert.Equal("8", e.AuthorId);
        Assert.Equal("T", e.Title);
        Assert.Equal(new[] { "1", "2" }, e.Tags);
        Assert.Equal(BotEvents.NewPost, e.ToFsmEvent().Name);
    }

    [Fact]
    public void GoBroadcastingParser_produces_GoBroadcastingEvent()
    {
        var e = Assert.IsType<GoBroadcastingEvent>(new GoBroadcastingParser().Parse("{\"speakerId\":\"4\"}"));
        Assert.Equal("4", e.SpeakerId);
        Assert.Equal(BotEvents.GoBroadcasting, e.ToFsmEvent().Name);
    }

    [Fact]
    public void SpeakParser_produces_SpeakEvent()
    {
        var e = Assert.IsType<SpeakEvent>(new SpeakParser().Parse("{\"speakerId\":\"4\",\"content\":\"大家早安\"}"));
        Assert.Equal("4", e.SpeakerId);
        Assert.Equal("大家早安", e.Content);
    }

    [Fact]
    public void StopBroadcastingParser_produces_StopBroadcastingEvent()
    {
        var e = Assert.IsType<StopBroadcastingEvent>(new StopBroadcastingParser().Parse("{\"speakerId\":\"4\"}"));
        Assert.Equal("4", e.SpeakerId);
    }

    [Fact]
    public void LoginParser_produces_LoginEvent_with_isAdmin()
    {
        var e = Assert.IsType<LoginEvent>(new LoginParser().Parse("{\"userId\":\"1\",\"isAdmin\":true}"));
        Assert.Equal("1", e.UserId);
        Assert.True(e.IsAdmin);
    }

    [Fact]
    public void LogoutParser_produces_LogoutEvent()
    {
        var e = Assert.IsType<LogoutEvent>(new LogoutParser().Parse("{\"userId\":\"1\"}"));
        Assert.Equal("1", e.UserId);
    }

    [Fact]
    public void StartedParser_produces_StartedEvent_with_quota()
    {
        var e = Assert.IsType<StartedEvent>(new StartedParser().Parse("{\"time\":\"2023-08-07 00:00:00\",\"quota\":10}"));
        Assert.Equal(10, e.Quota);
    }

    [Fact]
    public void EndParser_produces_EndEvent()
    {
        Assert.IsType<EndEvent>(new EndParser().Parse(""));
    }

    [Theory]
    [InlineData("10 seconds", 10)]
    [InlineData("2 minutes", 120)]
    [InlineData("1 hours", 3600)]
    public void ElapsedParser_converts_unit_to_seconds(string shell, int expected)
    {
        var e = Assert.IsType<ElapsedEvent>(new ElapsedParser().Parse(shell));
        Assert.Equal(expected, e.Seconds);
        Assert.Equal(expected, e.ToFsmEvent().Payload); // FSM payload = 換算後 seconds
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
        Assert.IsType<LoginEvent>(e);
    }

    [Fact]
    public void Dispatcher_routes_elapsed_shell_and_converts_seconds()
    {
        var e = Assert.IsType<ElapsedEvent>(Dispatcher().Parse("[10 seconds elapsed]"));
        Assert.Equal(10, e.Seconds);
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
        var fake = new FakeParser("custom", new FakeEvent());
        var dispatcher = new EventParser(new IEventParser[] { fake });

        var e = dispatcher.Parse("[custom] {}");

        Assert.Same(fake.Result, e);
    }

    private sealed class FakeParser(string name, IDomainEvent result) : IEventParser
    {
        public IDomainEvent Result => result;
        public string Name => name;
        public IDomainEvent Parse(string contentJson) => result;
    }

    private sealed record FakeEvent : IDomainEvent
    {
        public Event ToFsmEvent() => new("custom");
    }
}
