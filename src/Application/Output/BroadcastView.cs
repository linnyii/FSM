namespace Application.Output;

/// <summary>廣播頻道的輸出格式:機器人 <c>🤖 go broadcasting...</c> / <c>🤖 speaking:</c> / <c>🤖 stop broadcasting...</c>。</summary>
public sealed class BroadcastView(TextWriter output)
{
    public void BotStarts() => output.WriteLine("🤖 go broadcasting...");

    public void BotSpeaks(string content) => output.WriteLine($"🤖 speaking: {content}");

    public void BotStops() => output.WriteLine("🤖 stop broadcasting...");
}
