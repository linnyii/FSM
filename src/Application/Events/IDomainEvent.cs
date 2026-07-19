using Fsm.Core;

namespace Application.Events;

public interface IDomainEvent
{
    void ConsoleOutput(TextWriter output) { }
    void ApplyCustomizedEventInfoTo(BotContext ctx) { }
    Event ToFsmEvent();
}
