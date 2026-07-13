using Application.Events;

namespace Application.Parsing;

public interface IEventParser
{
    string Name { get; }

    IDomainEvent Parse(string contentJson);
}

public sealed class UnknownEventException(string name)
    : Exception($"No parser registered for event name: '{name}'")
{
    public string EventName { get; } = name;
}
