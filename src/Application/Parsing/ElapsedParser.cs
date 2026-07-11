using Bot;
using Fsm.Core;

namespace Application.Parsing;

/// <summary>
/// <c>[&lt;n&gt; &lt;unit&gt; elapsed]</c> —— 秒數在外殼裡(無 json)。unit ∈ {seconds, minutes, hours},
/// 換算成 seconds 放進 payload。分派器偵測到 elapsed 外殼時,把 <c>"&lt;n&gt; &lt;unit&gt;"</c> 當 json 傳入。
/// </summary>
public sealed class ElapsedParser : IEventParser
{
    public string Name => BotEvents.Elapsed;

    /// <summary>傳入 "&lt;n&gt; &lt;unit&gt;"(例 "10 seconds"),產出 Event(elapsed, seconds)。</summary>
    public Event Parse(string shell)
    {
        var parts = shell.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var n))
            throw new FormatException($"Invalid elapsed shell: '{shell}'. Expected '<n> <unit>'.");

        var seconds = parts[1].ToLowerInvariant() switch
        {
            "seconds" => n,
            "minutes" => n * 60,
            "hours" => n * 3600,
            _ => throw new FormatException($"Unknown time unit: '{parts[1]}'."),
        };
        return new Event(BotEvents.Elapsed, seconds);
    }
}
