namespace Fsm.Core;

/// <summary>
/// 普通（非子狀態機）狀態。entry/exit/handle 可由 client 用委派注入。
/// handle 型的響應（輪播）<b>不消化事件</b>（回 <see cref="FireResult.NotConsumed"/>）——
/// 做完響應後外層仍要查 transition 表（呼應「先響應再轉移」）。
/// </summary>
public class LeafState<C>(
    string id,
    Action<C>? onEntry = null,
    Action<C>? onExit = null,
    Action<Event, C>? onHandle = null)
    : IState<C>
{
    public string Id { get; } = id;

    public virtual void OnEntry(C ctx) => onEntry?.Invoke(ctx);

    public virtual void OnExit(C ctx) => onExit?.Invoke(ctx);

    public virtual FireResult Handle(Event @event, C ctx)
    {
        onHandle?.Invoke(@event, ctx);
        // leaf 的響應（輪播）不轉移 → 不消化，讓外層繼續查表。
        return FireResult.NotConsumed;
    }
}
