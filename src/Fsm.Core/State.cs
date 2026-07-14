namespace Fsm.Core;


public interface IState<in C>
{
    string Id { get; }

    void OnEntry(C ctx);

    void OnExit(C ctx);
    TriggerResult Handle(Event @event, C ctx);
}
