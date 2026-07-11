using Application;
using Application.Parsing;

var messenger = new ConsoleMessenger();
var ctx = new BotContext(messenger, initialTokenQuota: 100);
var fsm = WaterballBot.Define();

// 註冊表式分派器:收集所有小 parser,依 [name] 查表分派。
var parser = new EventParser(new IEventParser[]
{
    new NewMessageParser(),
    new NewPostParser(),
    new GoBroadcastingParser(),
    new SpeakParser(),
    new StopBroadcastingParser(),
    new LoginParser(),
    new LogoutParser(),
    new ElapsedParser(),
    new StartedParser(),
    new EndParser(),
});

// 範例事件列表寫死在此跑(demo),不從 stdin 讀。
string[] script =
[
    "[started] {\"time\":\"2023-08-07 00:00:00\",\"quota\":100}",
    "[login] {\"userId\":\"1\",\"isAdmin\":true}",
    "[login] {\"userId\":\"2\",\"isAdmin\":false}",
    "[new message] {\"authorId\":\"1\",\"content\":\"king\",\"tags\":[\"bot\"]}",
    "[10 seconds elapsed]",
    "[new message] {\"authorId\":\"1\",\"content\":\"king-stop\",\"tags\":[\"bot\"]}",
    "[end]",
];

// 進場:啟動初始狀態(依線上人數選 Default/Interacting)。
fsm.Current.OnEntry(ctx);

foreach (var line in script)
{
    var @event = parser.Parse(line);
    if (@event is null)
        continue;
    if (@event.Name == "end")
        break;

    EventContextBinder.Apply(@event, ctx);
    fsm.Fire(@event, ctx);
}
