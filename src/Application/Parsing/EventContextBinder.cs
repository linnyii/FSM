using Bot;
using Fsm.Core;

namespace Application.Parsing;

/// <summary>
/// 在 <c>fsm.Fire</c> 之前,依事件 payload 更新 ctx 的使用者/額度狀態 ——
/// 讓 guard(讀 <c>CurrentUser.IsAdmin</c>)與 transition 看到最新黑板。
/// parser 保持純粹(只產 Event),ctx 變更集中在此。
/// </summary>
public static class EventContextBinder
{
    public static void Apply(Event @event, BotContext ctx)
    {
        switch (@event.Payload)
        {
            case LoginInfo login:
                ctx.UpsertUser(login.UserId, login.IsAdmin);
                ctx.CurrentUser = ctx.Users[login.UserId];
                break;

            case StartedInfo started:
                ctx.SeedQuota(started.Quota);
                break;

            case ChatMessage msg:
                ctx.SetCurrentUser(msg.AuthorId);
                break;

            case NewPost post:
                ctx.SetCurrentUser(post.AuthorId);
                break;
        }
    }
}
