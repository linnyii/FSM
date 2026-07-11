namespace Application.Parsing;

/// <summary>
/// 各事件的強型別 payload record —— 放進 <see cref="Fsm.Core.Event.Payload"/>，
/// 下游 guard/action 用 <c>payload is XxxInfo</c> 依型別分流。
/// 聊天訊息沿用 bot 層既有的 <see cref="Bot.ChatMessage"/>，不在此重定義。
/// </summary>
public sealed record NewPost(string Id, string AuthorId, string Title, string Content, IReadOnlyList<string> Tags);

public sealed record SpeakInfo(string SpeakerId, string Content);

public sealed record BroadcastInfo(string SpeakerId);

public sealed record LoginInfo(string UserId, bool IsAdmin);

public sealed record LogoutInfo(string UserId);

public sealed record StartedInfo(string Time, int Quota);
