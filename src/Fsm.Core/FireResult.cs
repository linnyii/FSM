namespace Fsm.Core;

/// <summary>
/// <c>fire()</c> / <c>handle()</c> 的回傳：這一層（含子層）有沒有真的發生一次 transition。
/// consumed 是「單層 FSM 本來就該有的通用能力」，冒泡才有依據；
/// 不是為子狀態機加的特判（核心裡沒有任何 <c>if (composite)</c>）。
/// </summary>
public enum FireResult
{
    /// <summary>這一層（或子層）真的觸發了一次 transition。</summary>
    Consumed,

    /// <summary>沒有任何 transition 發生——冒泡讓外層自己查表。</summary>
    NotConsumed,
}
