namespace Fsm.Core;

/// <summary>
/// 一個狀態：本狀態下進場、出場、事件響應怎麼做。
/// State 只負責「本狀態下的響應行為」，<b>不負責決定轉去哪</b>（轉移寫在 Transition）。
/// </summary>
/// <remarks>
/// <see cref="Handle"/> 是狀態內、<b>不轉移</b>的響應（輪播等），與轉移的 action 分開——
/// <c>king @bot</c> 先 handle 回 "good to hear"，再走 transition 進知識王，證明兩者必須分開。
/// </remarks>
public interface IState<in C>
{
    /// <summary>身份，transition 用它指涉（宣告式資料引用「名字」而非「實體」）。</summary>
    string Id { get; }

    /// <summary>進場動作（進 Normal reset 輪播 index、進 Record 看有無廣播）。</summary>
    void OnEntry(C ctx);

    /// <summary>出場動作。</summary>
    void OnExit(C ctx);

    /// <summary>
    /// 本狀態下事件的「響應行為」。回傳 consumed 語意統一為「我這層（含子層）有沒有發生 transition」。
    /// leaf 的輪播型 handle 不消化事件（回 NotConsumed），做完響應外層照樣查 transition 表。
    /// </summary>
    FireResult Handle(Event @event, C ctx);
}
