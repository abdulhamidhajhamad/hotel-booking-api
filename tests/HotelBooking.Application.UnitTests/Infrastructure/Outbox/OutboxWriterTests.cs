using System.Text.Json;
using HotelBooking.Application.Features.Auth.Common;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Infrastructure.Outbox;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.UnitTests.Infrastructure.Outbox;

public class OutboxWriterTests : IAsyncDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    private OutboxWriter Sut => new(_db);

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EnqueueAsync_AddsPendingRowWithSerializedPayload()
    {
        var @event = new UserRegisteredEvent(
            Guid.NewGuid(),
            "abc@test.com",
            "abc",
            "raw-token");

        await Sut.EnqueueAsync(@event, CancellationToken.None);
        await _db.SaveChangesAsync();

        var row = await _db.OutboxMessages.SingleAsync();

        row.Type.Should().Be(typeof(UserRegisteredEvent).FullName);
        row.Status.Should().Be(OutboxMessageStatus.Pending);
        row.AttemptCount.Should().Be(0);
        row.ProcessedAtUtc.Should().BeNull();
        row.LastError.Should().BeNull();
        row.OccurredAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        row.NextAttemptAtUtc.Should().Be(row.OccurredAtUtc);

        using var payload = JsonDocument.Parse(row.Payload);
        var root = payload.RootElement;
        root.GetProperty("userId").GetGuid().Should().Be(@event.UserId);
        root.GetProperty("email").GetString().Should().Be(@event.Email);
        root.GetProperty("userName").GetString().Should().Be(@event.UserName);
        root.GetProperty("confirmationToken").GetString().Should().Be(@event.ConfirmationToken);
    }

    [Fact]
    public async Task EnqueueAsync_DoesNotSaveByItself()
    {
        var @event = new UserRegisteredEvent(Guid.NewGuid(), "x@test.com", "x", "t");

        await Sut.EnqueueAsync(@event, CancellationToken.None);

        (await _db.OutboxMessages.CountAsync()).Should().Be(0);

        _db.ChangeTracker.Entries<OutboxMessage>()
            .Should().ContainSingle(e => e.State == EntityState.Added);
    }

    [Fact]
    public async Task EnqueueAsync_MultipleEvents_AddsMultipleRows()
    {
        var e1 = new UserRegisteredEvent(Guid.NewGuid(), "a@test.com", "a", "t1");
        var e2 = new UserRegisteredEvent(Guid.NewGuid(), "b@test.com", "b", "t2");

        await Sut.EnqueueAsync(e1, CancellationToken.None);
        await Sut.EnqueueAsync(e2, CancellationToken.None);
        await _db.SaveChangesAsync();

        (await _db.OutboxMessages.CountAsync()).Should().Be(2);
    }
}