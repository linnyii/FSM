using Application;
using Application.Events;
using Application.Parsing;

var messenger = new BotConsoleMessenger();
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
    "[login] {\"userId\": \"5\", \"isAdmin\": false}",
    "[login] {\"userId\": \"6\", \"isAdmin\": false}",
    "[login] {\"userId\": \"7\", \"isAdmin\": false}",
    "[login] {\"userId\": \"8\", \"isAdmin\": false}",
    "[login] {\"userId\": \"9\", \"isAdmin\": false}",
    "[new message] {\"authorId\": \"1\", \"content\": \"10 人上線,進入互動模式\", \"tags\": []}",

    // KnowledgeKing/Questioning:admin 下 king(需 admin + 額度)→ 出第 0 題
    "[new message] {\"authorId\": \"1\", \"content\": \"king\", \"tags\": [\"bot\"]}",
    // 答對第 0 題 (A) → 進第 1 題;答對第 1 題 (C) → 進第 2 題
    "[new message] {\"authorId\": \"2\", \"content\": \"A\", \"tags\": [\"bot\"]}",
    "[new message] {\"authorId\": \"3\", \"content\": \"C\", \"tags\": [\"bot\"]}",
    // KnowledgeKing/ThanksForJoining:答對最後一題 (A) → 進 ThanksForJoining
    "[new message] {\"authorId\": \"4\", \"content\": \"A\", \"tags\": [\"bot\"]}",
    // ThanksForJoining 停留滿 20 秒 → 回 Normal
    "[20 seconds elapsed]",

    // Record/Waiting:下 record → 進 Record(初始 Waiting)
    "[new message] {\"authorId\": \"3\", \"content\": \"record\", \"tags\": [\"bot\"]}",
    // Record/Recording:有人開始廣播 → 進 Recording,錄下發言
    "[go broadcasting] {\"speakerId\": \"4\"}",
    "[speak] {\"speakerId\": \"4\", \"content\": \"大家早安\"}",
    "[stop broadcasting] {\"speakerId\": \"4\"}",
    // 錄音者下 stop-recording → 離開 Record 回 Normal
    "[new message] {\"authorId\": \"3\", \"content\": \"stop-recording\", \"tags\": [\"bot\"]}",

    "[end]",
];

fsm.CurrentState.OnEntry(ctx);

foreach (var line in eventsScript)
{
    var domainEvent = parser.Parse(line);
    if (domainEvent is null)
        continue;
    if (domainEvent is EndEvent)
        break;

    domainEvent.ConsoleOutput(Console.Out);    
    domainEvent.ApplyCustomizedEventInfoTo(ctx);               
    fsm.Process(domainEvent.ToFsmEvent(), ctx); 
}
