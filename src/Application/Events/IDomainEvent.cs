using Fsm.Core;

namespace Application.Events;

public interface IDomainEvent
{
    void Echo(TextWriter output) { }
    void ApplyCustomizedEventInfoTo(BotContext ctx) { }
    Event ToFsmEvent();
}
