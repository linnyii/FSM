namespace Application.Output;

public static class Tags
{
    public static string Format(IReadOnlyList<string>? tags) =>
        tags is { Count: > 0 } ? " " + string.Join(", ", tags.Select(t => $"@{t}")) : string.Empty;
}
