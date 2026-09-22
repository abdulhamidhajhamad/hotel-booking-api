using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Features.Auth.Common;
using HotelBooking.Infrastructure.Outbox;

namespace HotelBooking.Application.UnitTests.Infrastructure.Outbox;

public class OutboxEventTypeRegistryTests
{
    [Fact]
    public void TryResolve_KnownEvent_ReturnsMatchingType()
    {
        var sut = new OutboxEventTypeRegistry(typeof(UserRegisteredEvent).Assembly);

        var found = sut.TryResolve(typeof(UserRegisteredEvent).FullName!, out var type);

        found.Should().BeTrue();
        type.Should().Be(typeof(UserRegisteredEvent));
    }

    [Fact]
    public void TryResolve_UnknownName_ReturnsFalse()
    {
        var sut = new OutboxEventTypeRegistry(typeof(UserRegisteredEvent).Assembly);

        var found = sut.TryResolve("Nope.Not.Registered", out var type);

        found.Should().BeFalse();
        type.Should().BeNull();
    }

    [Fact]
    public void Constructor_SkipsAbstractAndInterfaceImplementers()
    {
        var sut = new OutboxEventTypeRegistry(typeof(OutboxEventTypeRegistryTests).Assembly);

        sut.TryResolve(typeof(IAbstractEvent).FullName!, out _).Should().BeFalse();
        sut.TryResolve(typeof(AbstractEventBase).FullName!, out _).Should().BeFalse();

        sut.TryResolve(typeof(ConcreteTestEvent).FullName!, out var type).Should().BeTrue();
        type.Should().Be(typeof(ConcreteTestEvent));
    }

    [Fact]
    public void Constructor_WithNoAssemblies_ResolvesNothing()
    {
        var sut = new OutboxEventTypeRegistry();

        sut.TryResolve(typeof(UserRegisteredEvent).FullName!, out _).Should().BeFalse();
    }

    public interface IAbstractEvent : IIntegrationEvent { }
    public abstract class AbstractEventBase : IIntegrationEvent { }
    public sealed record ConcreteTestEvent(string Value) : IIntegrationEvent;
}