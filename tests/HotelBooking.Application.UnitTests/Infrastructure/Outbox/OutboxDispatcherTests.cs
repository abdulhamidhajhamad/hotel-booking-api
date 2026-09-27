using System.Text.Json;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Infrastructure.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application.UnitTests.Infrastructure.Outbox;

public class OutboxDispatcherTests : IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly OutboxEventTypeRegistry _registry =
        new(typeof(OutboxDispatcherTests).Assembly);

    private readonly ServiceCollection _services = new();
    private ServiceProvider? _provider;

    private OutboxDispatcher Build()
    {
        _provider = _services.BuildServiceProvider();
        return new OutboxDispatcher(_registry, _provider, NullLogger<OutboxDispatcher>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider is not null)
            await _provider.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DispatchAsync_KnownType_InvokesRegisteredHandler()
    {
        var handler = new RecordingHandler();
        _services.AddSingleton<IOutboxHandler<TestEvent>>(handler);
        var sut = Build();

        var payload = JsonSerializer.Serialize(new TestEvent("hello"), SerializerOptions);

        await sut.DispatchAsync(typeof(TestEvent).FullName!, payload, CancellationToken.None);

        handler.Received.Should().ContainSingle();
        handler.Received[0].Message.Should().Be("hello");
    }

    [Fact]
    public async Task DispatchAsync_UnknownType_Throws()
    {
        var sut = Build();

        var act = () => sut.DispatchAsync("Nope.NotRegistered", "{}", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Nope.NotRegistered*");
    }

    [Fact]
    public async Task DispatchAsync_NoHandlersRegistered_ReturnsWithoutThrowing()
    {
        var sut = Build();

        var payload = JsonSerializer.Serialize(new TestEvent("x"), SerializerOptions);

        var act = () => sut.DispatchAsync(typeof(TestEvent).FullName!, payload, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_MultipleHandlers_InvokesAll()
    {
        var h1 = new RecordingHandler();
        var h2 = new RecordingHandler();
        _services.AddSingleton<IOutboxHandler<TestEvent>>(h1);
        _services.AddSingleton<IOutboxHandler<TestEvent>>(h2);
        var sut = Build();

        var payload = JsonSerializer.Serialize(new TestEvent("fan-out"), SerializerOptions);

        await sut.DispatchAsync(typeof(TestEvent).FullName!, payload, CancellationToken.None);

        h1.Received.Should().ContainSingle();
        h2.Received.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrows_PropagatesException()
    {
        _services.AddSingleton<IOutboxHandler<TestEvent>>(new ThrowingHandler());
        var sut = Build();

        var payload = JsonSerializer.Serialize(new TestEvent("boom"), SerializerOptions);

        var act = () => sut.DispatchAsync(typeof(TestEvent).FullName!, payload, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*boom*");
    }

    public sealed record TestEvent(string Message) : IIntegrationEvent;

    private sealed class RecordingHandler : IOutboxHandler<TestEvent>
    {
        public List<TestEvent> Received { get; } = new();

        public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken)
        {
            Received.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IOutboxHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken)
            => Task.FromException(new InvalidOperationException($"boom: {integrationEvent.Message}"));
    }
}