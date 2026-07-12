namespace Application.Parsing;

/// <summary>
/// FSM payload record —— 由 <see cref="Application.Events.IDomainEvent.ToFsmEvent"/> 放進
/// <see cref="Fsm.Core.Event.Payload"/>，下游 guard/action 用 <c>payload is XxxInfo</c> 依型別分流。
/// 聊天訊息沿用 bot 層既有的 <see cref="Bot.ChatMessage"/>，不在此重定義。
/// (login/logout/started/end 的 FSM 事件無 payload —— 靠事件名觸發,故不需對應 record。)
/// </summary>
public sealed record NewPost(string Id, string AuthorId, string Title, string Content, IReadOnlyList<string> Tags);

public sealed record SpeakInfo(string SpeakerId, string Content);

public sealed record BroadcastInfo(string SpeakerId);
