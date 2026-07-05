namespace Fsm.Core;

public sealed class Event(string name, object? payload = null)
{
    public string Name { get; } = name;
    public object? Payload { get; } = payload;
}
