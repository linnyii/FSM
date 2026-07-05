using Bot;
using Fsm.Core;

namespace Application;

/// <summary>
/// 把輸入的每一行 parse 成 <see cref="Event"/>(name, payload)。
/// 「輸入的每一行都是 Event」——訊息只是事件的一種。
/// 也負責在有作者資訊時，更新 ctx 的「當前發話者是否 admin」（供 .adminOnly() guard 讀）。
/// </summary>
public sealed class EventParser
{
    private readonly IReadOnlySet<int> _adminUserIds;

    public EventParser(IReadOnlySet<int> adminUserIds) => _adminUserIds = adminUserIds;

    /// <summary>
    /// 支援的輸入語法（示範用，可依實際輸入格式調整）：
    /// <list type="bullet">
    /// <item><c>login</c> / <c>logout</c></item>
    /// <item><c>go broadcasting</c> / <c>stop-recording</c> / <c>elapsed</c></item>
    /// <item><c>msg &lt;authorId&gt; [@bot] &lt;content...&gt;</c> —— 一則聊天訊息</item>
    /// </list>
    /// 回傳 null 表示這行看不懂、略過。
    /// </summary>
    public Event? Parse(string line, BotContext ctx)
    {
        line = line.Trim();
        if (line.Length == 0)
            return null;

        switch (line)
        {
            case WaterballBot.Login:
            case WaterballBot.Logout:
            case WaterballBot.GoBroadcasting:
            case WaterballBot.StopRecording:
            case WaterballBot.Elapsed:
                return new Event(line);
        }

        if (line.StartsWith("msg ", StringComparison.Ordinal))
            return ParseMessage(line["msg ".Length..], ctx);

        return null;
    }

    private Event ParseMessage(string rest, BotContext ctx)
    {
        var parts = rest.Split(' ', 2, StringSplitOptions.TrimEntries);
        var authorId = int.TryParse(parts[0], out var id) ? id : -1;
        var body = parts.Length > 1 ? parts[1] : string.Empty;

        var tagsBot = false;
        if (body.StartsWith("@bot ", StringComparison.Ordinal))
        {
            tagsBot = true;
            body = body["@bot ".Length..];
        }
        else if (body == "@bot")
        {
            tagsBot = true;
            body = string.Empty;
        }

        // 更新「當前發話者是否 admin」，讓 .adminOnly() guard 讀得到。
        ctx.IsCurrentUserAdmin = _adminUserIds.Contains(authorId);

        return new Event(BotEvents.NewMessage, new ChatMessage(authorId, body, tagsBot));
    }
}
