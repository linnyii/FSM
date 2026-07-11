using Fsm.Core;

namespace Application.Parsing;

/// <summary>
/// 分派器:讀 <c>[&lt;name&gt;] &lt;json&gt;</c> 外殼、抽 json,依 name 查表分派給對應小 parser(註冊表式 Strategy)。
/// 選擇邏輯用 Dictionary(非 switch)—— 加新事件只需多一個 <see cref="IEventParser"/> 實作,分派器不動。
/// </summary>
public sealed class EventParser
{
    private readonly Dictionary<string, IEventParser> _parsers;

    public EventParser(IEnumerable<IEventParser> parsers) =>
        _parsers = parsers.ToDictionary(p => p.Name);

    /// <summary>
    /// Parse 一行輸入。回傳 null 表示空行(略過)。未知 name → <see cref="UnknownEventException"/>。
    /// </summary>
    public Event? Parse(string line)
    {
        line = line.Trim();
        if (line.Length == 0)
            return null;

        var (shell, json) = SplitShell(line);

        // elapsed 特例:外殼是 "<n> <unit> elapsed",秒數在外殼裡、無 json。
        if (shell.EndsWith(" elapsed", StringComparison.Ordinal) || shell == "elapsed")
        {
            var inner = shell[..^"elapsed".Length].Trim(); // "<n> <unit>"
            return Resolve("elapsed").Parse(inner);
        }

        return Resolve(shell).Parse(json);
    }

    private IEventParser Resolve(string name) =>
        _parsers.TryGetValue(name, out var p) ? p : throw new UnknownEventException(name);

    // "[name] json" → (name, json)。json 可為空(無 payload 事件)。
    private static (string Shell, string Json) SplitShell(string line)
    {
        var close = line.IndexOf(']');
        if (!line.StartsWith('[') || close < 0)
            throw new FormatException($"Malformed event line: '{line}'. Expected '[name] json'.");

        var shell = line[1..close].Trim();
        var json = line[(close + 1)..].Trim();
        return (shell, json);
    }
}
