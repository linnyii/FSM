namespace Application.Output;

/// <summary>tags 格式:每個 id 前標 <c>@</c>,以「逗號 + 一個空白」分隔(<c>@3, @4, @bot</c>)。</summary>
internal static class Tags
{
    public static string Format(IReadOnlyList<string>? tags) =>
        tags is { Count: > 0 } ? " " + string.Join(", ", tags.Select(t => $"@{t}")) : string.Empty;
}
