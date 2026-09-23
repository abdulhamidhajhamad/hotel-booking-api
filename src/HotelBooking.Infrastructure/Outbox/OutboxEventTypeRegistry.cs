using System.Reflection;
using HotelBooking.Application.Abstractions.Outbox;

namespace HotelBooking.Infrastructure.Outbox;

public sealed class OutboxEventTypeRegistry
{
    private readonly Dictionary<string, Type> _byName;

    public OutboxEventTypeRegistry(params Assembly[] assemblies)
    {
        _byName = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (!typeof(IIntegrationEvent).IsAssignableFrom(type))
                    continue;

                _byName[type.FullName!] = type;
            }
        }
    }

    public bool TryResolve(string typeName, out Type type)
    {
        return _byName.TryGetValue(typeName, out type!);
    }
}
