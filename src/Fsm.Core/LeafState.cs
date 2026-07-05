namespace Fsm.Core;

/// <summary>
/// 普通（非子狀態機）狀態。entry/exit/handle 可由 client 用委派注入。
/// handle 型的響應（輪播）<b>不消化事件</b>（回 <see cref="FireResult.NotConsumed"/>）——
/// 做完響應後外層仍要查 transition 表（呼應「先響應再轉移」）。
/// </summary>
public class LeafState<C> : IState<C>
{
    private readonly Action<C>? _onEntry;
    private readonly Action<C>? _onExit;
    private readonly Action<Event, C>? _onHandle;

    public string Id { get; }

    public LeafState(
        string id,
        Action<C>? onEntry = null,
        Action<C>? onExit = null,
        Action<Event, C>? onHandle = null)
    {
        Id = id;
        _onEntry = onEntry;
        _onExit = onExit;
        _onHandle = onHandle;
    }

    public virtual void OnEntry(C ctx) => _onEntry?.Invoke(ctx);

    public virtual void OnExit(C ctx) => _onExit?.Invoke(ctx);

    public virtual FireResult Handle(Event @event, C ctx)
    {
        _onHandle?.Invoke(@event, ctx);
        // leaf 的響應（輪播）不轉移 → 不消化，讓外層繼續查表。
        return FireResult.NotConsumed;
    }
}
