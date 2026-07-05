using Application;


var messenger = new ConsoleMessenger();           // 具體實作，懂 I/O 格式
var ctx = new BotContext(messenger, initialQuota: 100);
var fsm = WaterballBot.Define();                  // 用 bot module DSL 宣告 Waterball
var parser = new EventParser(adminUserIds: new HashSet<int> { 1 });

// 進場：啟動初始狀態（依線上人數選 Default/Interacting）。
fsm.Current.OnEntry(ctx);

string? line;
while ((line = Console.ReadLine()) is not null)
{
    var @event = parser.Parse(line, ctx);
    if (@event is null)
        continue;

    fsm.Fire(@event, ctx);
}
