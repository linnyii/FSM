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
    "[login] {\"userId\":\"3\",\"isAdmin\":false}",
    "[login] {\"userId\":\"4\",\"isAdmin\":false}",
    // 錄音流程(對照使用者提供範例):3 下 record,4 廣播,stop broadcasting → Record Replay @3
    "[new message] {\"authorId\":\"3\",\"content\":\"record\",\"tags\":[\"bot\"]}",
    "[go broadcasting] {\"speakerId\":\"4\"}",
    "[speak] {\"speakerId\":\"4\",\"content\":\"大家好,我是小華!\"}",
    "[speak] {\"speakerId\":\"4\",\"content\":\"歡迎來到小華脫口秀\"}",
    "[stop broadcasting] {\"speakerId\":\"4\"}",
    // 3 自己補一段,再 stop-recording → Record Replay @3 + 回 Normal
    "[go broadcasting] {\"speakerId\":\"3\"}",
    "[speak] {\"speakerId\":\"3\",\"content\":\"我再來補一個笑話!\"}",
    "[new message] {\"authorId\":\"3\",\"content\":\"stop-recording\",\"tags\":[\"bot\"]}",
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
