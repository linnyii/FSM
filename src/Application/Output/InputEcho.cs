using Application.Parsing;
using Bot;
using Fsm.Core;

namespace Application.Output;

/// <summary>
/// 輸入端回顯:把 incoming 成員事件印成正式格式(💬 聊天 / 論壇發文 / 📢 廣播 / 🕑 時間流逝)。
/// 在 <c>fsm.Fire</c> 之前呼叫 —— 先顯示「誰做了什麼」,機器人回應再接在後面。
/// login/logout/started/end 不回顯。
/// </summary>
public sealed class InputEcho(TextWriter output)
{
    public void Echo(Event @event, string rawLine)
    {
        switch (@event.Payload)
        {
            case ChatMessage m:
                output.WriteLine($"💬 {m.AuthorId}: {m.Content}{Tags.Format(m.Tags)}");
                break;

            case NewPost p:
                output.WriteLine($"{p.AuthorId}:【{p.Title}】{p.Content}{Tags.Format(p.Tags)}");
                break;

            case SpeakInfo s:
                output.WriteLine($"📢 {s.SpeakerId}: {s.Content}");
                break;

            case BroadcastInfo b when @event.Name == BotEvents.GoBroadcasting:
                output.WriteLine($"📢 {b.SpeakerId} is broadcasting...");
                break;

            case BroadcastInfo b when @event.Name == BotEvents.StopBroadcasting:
                output.WriteLine($"📢 {b.SpeakerId} stop broadcasting");
                break;

            case int when @event.Name == BotEvents.Elapsed:
                // elapsed payload 只有換算後 seconds;回顯要原始 "<n> <unit>",從輸入外殼取。
                output.WriteLine($"🕑 {ElapsedShell(rawLine)} elapsed...");
                break;
        }
    }

    // "[10 seconds elapsed]" → "10 seconds"。
    private static string ElapsedShell(string rawLine)
    {
        var close = rawLine.IndexOf(']');
        var shell = rawLine[1..close].Trim();          // "10 seconds elapsed"
        return shell[..^"elapsed".Length].Trim();       // "10 seconds"
    }
}
