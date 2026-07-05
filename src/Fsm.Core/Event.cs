namespace Fsm.Core;

/// <summary>
/// 一件發生的事 + 它攜帶的資料。輸入的每一行都是一個 Event
/// （"new message" / "login" / "go broadcasting" / "elapsed" ...）。
/// </summary>
/// <remarks>
/// FSM 用 <see cref="Name"/> 對 transition 的 <c>on</c> 做匹配（不是 eventId——
/// 同種事件會發生無數次、共用同一個名字）。<see cref="Payload"/> 給 Guard/Action 判斷用。
/// </remarks>
public sealed class Event
{
    public string Name { get; }
    public object? Payload { get; }

    public Event(string name, object? payload = null)
    {
        Name = name;
        Payload = payload;
    }
}
