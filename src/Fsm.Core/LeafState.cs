namespace Fsm.Core;

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

    public virtual TriggerResult Handle(Event @event, C ctx)
    {
        onHandle?.Invoke(@event, ctx);
        // leaf 的響應（輪播）不轉移 → 不消化，讓外層繼續查表。
        return TriggerResult.NotConsumed;
    }
}
