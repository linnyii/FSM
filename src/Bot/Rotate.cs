using Fsm.Core;

namespace Bot;

public sealed class Rotate<C>(params string[] messages)
    where C : IBotContext
{
    private readonly IReadOnlyList<string> _messages = messages;
    private int _index;

    private void ResetOnEntry(C ctx) => _index = 0;

    private void RotateMessage(Event @event, C ctx)
    {
        if (_messages.Count == 0)
            return;

        var content = _messages[_index % _messages.Count];
        _index++;
        ctx.Messenger.SendChat(content);
    }

    public Action<Event, C> DecorateHandle(Action<Event, C>? onHandle) =>
        onHandle is null
            ? RotateMessage
            : (e, ctx) => { onHandle(e, ctx); RotateMessage(e, ctx); };

    public Action<C> DecorateEntry(Action<C>? onEntry) =>
        ctx => { onEntry?.Invoke(ctx); ResetOnEntry(ctx); };
}
