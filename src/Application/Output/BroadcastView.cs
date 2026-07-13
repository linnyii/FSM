namespace Application.Output;

public sealed class BroadcastView(TextWriter output)
{
    public void BotStarts() => output.WriteLine("🤖 go broadcasting...");

    public void BotSpeaks(string content) => output.WriteLine($"🤖 speaking: {content}");

    public void BotStops() => output.WriteLine("🤖 stop broadcasting...");
}
