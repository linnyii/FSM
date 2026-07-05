namespace Fsm.Core;

public sealed class Transition<C>(
    string from,
    string on,
    string to,
    IGuard<C>? guard = null,
    IAction<C>? action = null)
{
    public string From { get; } = from;
    public string On { get; } = on;
    public IGuard<C> Guard { get; } = guard ?? AlwaysTrueGuard<C>.Instance;
    public IAction<C> Action { get; } = action ?? NoOpAction<C>.Instance;
    public string To { get; } = to;
}
