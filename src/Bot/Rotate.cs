using Fsm.Core;

namespace Bot;

/// <summary>
/// 輪播機制：一組訊息輪流回、每次「重新進入」該狀態從第一則開始。
/// 「留同存異」——異=訊息內容（application 給），同=循環索引 + 重進場歸零（bot module 內建）。
/// 循環 index 是該狀態<b>私有</b>資料（沒有別的 State 讀它），住在這個物件裡完全合理（設計第 2 節判準）。
/// </summary>
public sealed class Rotate<C> where C : IBotContext
{
    private readonly IReadOnlyList<string> _messages;
    private int _index;

    public Rotate(params string[] messages) => _messages = messages;

    /// <summary>掛到 state.onEntry：重進場歸零（示範 entry action 用途）。</summary>
    public void ResetOnEntry(C ctx) => _index = 0;

    /// <summary>掛到 state.handle：發下一則、索引前進（循環）。不轉移 → LeafState 回 NotConsumed。</summary>
    public void Emit(Event @event, C ctx)
    {
        if (_messages.Count == 0)
            return;

        var content = _messages[_index % _messages.Count];
        _index++;
        ctx.Messenger.SendChat(content);
    }

    /// <summary>把使用者的 onHandle 裝飾起來：先跑使用者的（若有），再吐輪播。</summary>
    public Action<Event, C> DecorateHandle(Action<Event, C>? inner) =>
        inner is null
            ? Emit
            : (e, ctx) => { inner(e, ctx); Emit(e, ctx); };

    /// <summary>把使用者的 onEntry 裝飾起來：先跑使用者的（若有），再把輪播索引歸零。</summary>
    public Action<C> DecorateEntry(Action<C>? inner) =>
        ctx => { inner?.Invoke(ctx); ResetOnEntry(ctx); };
}
