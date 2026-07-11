using Fsm.Core;

namespace Application.Parsing;

/// <summary>
/// 單一事件的 parser（Strategy）。每個實作宣告自己負責的 <see cref="Name"/>，
/// 把該事件的 json payload 反序列化成 <see cref="Event"/>（name + 強型別 payload record）。
/// 分派器 <see cref="EventParser"/> 依 Name 建 Dictionary、查表分派 —— 加新事件只需加一個實作。
/// </summary>
public interface IEventParser
{
    /// <summary>我負責哪個 <c>[name]</c>（例："login"）。</summary>
    string Name { get; }

    /// <summary>吃自己那包 json（無 payload 事件給空字串），產出 <see cref="Event"/>。</summary>
    Event Parse(string json);
}

/// <summary>輸入的 <c>[name]</c> 沒有對應 parser。明確報錯，不靜默吞掉。</summary>
public sealed class UnknownEventException(string name)
    : Exception($"No parser registered for event name: '{name}'")
{
    public string EventName { get; } = name;
}
