using Application;
using Application.Events;
using Application.Parsing;

var messenger = new ConsoleMessenger();
var ctx = new BotContext(messenger, initialTokenQuota: 100);
var fsm = WaterballBot.Define();

var parser = new EventParser([
    new NewMessageParser(),
    new NewPostParser(),
    new GoBroadcastingParser(),
    new SpeakParser(),
    new StopBroadcastingParser(),
    new LoginParser(),
    new LogoutParser(),
    new ElapsedParser(),
    new StartedParser(),
    new EndParser()
]);

string[] eventsScript =
[
    "[login] {\"userId\": \"1\", \"isAdmin\": true}",
    "[login] {\"userId\": \"2\", \"isAdmin\": false}",
    "[login] {\"userId\": \"3\", \"isAdmin\": false}",
    "[login] {\"userId\": \"4\", \"isAdmin\": false}",
    "[3 seconds elapsed]",
    "[login] {\"userId\": \"5\", \"isAdmin\": false}",
    "[login] {\"userId\": \"6\", \"isAdmin\": false}",
    "[new message] {\"authorId\": \"1\", \"content\": \"大家早安,今天我第一天上班呢\", \"tags\": []}",
    "[login] {\"userId\": \"7\", \"isAdmin\": false}",
    "[new message] {\"authorId\": \"4\", \"content\": \"祝大家今天事事順利\", \"tags\": [\"1\"]}",
    "[login] {\"userId\": \"8\", \"isAdmin\": false}",
    "[login] {\"userId\": \"9\", \"isAdmin\": false}",
    "[10 seconds elapsed]",
    "[new message] {\"authorId\": \"1\", \"content\": \"wow 有 10 個人在線上了呢(包含機器人)\", \"tags\": []}",
    "[new message] {\"authorId\": \"1\", \"content\": \"大家早安,今天要吃麥當勞嗎?\", \"tags\": []}",
    "[new message] {\"authorId\": \"8\", \"content\": \"發了一個文,分享笑話,哈哈\", \"tags\": []}",
    "[new post]  {\"id\": \"1\", \"authorId\": \"8\", \"title\": \"分享一個關於 單一職責原則 的笑話,每次講起來都還是覺得很好笑\", \"content\": \"(1) 欸你這個類別這樣做太多事了吧,違反單一職責原則啊,每個類別只能有一個職責,只能做一件事。 (2) 這個類別,確實只做一件事,那就是實現需求!\", \"tags\": [\"1\", \"2\", \"3\"]}",
    "[new message] {\"authorId\": \"1\", \"content\": \"king\", \"tags\": [\"bot\"]}",
    "[new message] {\"authorId\": \"6\", \"content\": \"A\", \"tags\": [\"bot\"]}",
    "[new message] {\"authorId\": \"8\", \"content\": \"C\", \"tags\": [\"bot\"]}",
    "[3 seconds elapsed]",
    "[new message] {\"authorId\": \"3\", \"content\": \"C\", \"tags\": [\"bot\"]}",
    "[new message] {\"authorId\": \"2\", \"content\": \"A\", \"tags\": [\"bot\"]}",
    "[20 seconds elapsed]",
    "[new message] {\"authorId\": \"3\", \"content\": \"record\", \"tags\": [\"bot\"]}",
    "[go broadcasting] {\"speakerId\": \"4\"}",
    "[speak] {\"speakerId\": \"4\", \"content\": \"大家早安\"}",
    "[speak] {\"speakerId\": \"4\", \"content\": \"各位有吃早餐嗎?\"}",
    "[stop broadcasting] {\"speakerId\": \"4\"}",
    "[new message] {\"authorId\": \"3\", \"content\": \"stop-recording\", \"tags\": [\"bot\"]}",
    "[logout] {\"userId\": \"9\"}",
    "[logout] {\"userId\": \"8\"}",
    "[3 seconds elapsed]",
    "[logout] {\"userId\": \"7\"}",
    "[logout] {\"userId\": \"6\"}",
    "[new message] {\"authorId\": \"1\", \"content\": \"呀,大家下線了\", \"tags\": []}",
    "[end]",
];

fsm.Current.OnEntry(ctx);

foreach (var line in eventsScript)
{
    var domainEvent = parser.Parse(line);
    if (domainEvent is null)
        continue;
    if (domainEvent is EndEvent)
        break;

    domainEvent.Echo(Console.Out);    
    domainEvent.ApplyCustomizedEventInfoTo(ctx);               
    fsm.Fire(domainEvent.ToFsmEvent(), ctx); // 轉成 FSM 事件跑機器人回應(🤖)
}
