using Fsm.Core;

namespace Application.Events;

/// <summary>
/// 一個領域事件 —— 把「同一個 event 該做的所有事」聚在一個型別裡:
/// <list type="bullet">
/// <item><see cref="Echo"/>:把成員回顯印到 output(💬/📢/🕑…);不回顯的事件(login/logout/started/end)空實作。</item>
/// <item><see cref="ApplyTo"/>:套用到 ctx 黑板(建使用者、設額度、設發話者…)。</item>
/// <item><see cref="ToFsmEvent"/>:轉成 FSM <c>Fire</c> 吃的 <see cref="Event"/>(name + payload)。</item>
/// </list>
/// 三個方法都是「event 自己執行副作用」—— 靠多型分派,主迴圈無 switch/case,加新事件只需新增一個實作。
/// </summary>
public interface IDomainEvent
{
    /// <summary>把成員回顯印到 output(預設不印 —— 給不回顯的事件搭便車)。</summary>
    void Echo(TextWriter output) { }

    /// <summary>套用到 ctx(預設不動黑板)。</summary>
    void ApplyTo(BotContext ctx) { }

    /// <summary>轉成 FSM 吃的 Event。</summary>
    Event ToFsmEvent();
}
