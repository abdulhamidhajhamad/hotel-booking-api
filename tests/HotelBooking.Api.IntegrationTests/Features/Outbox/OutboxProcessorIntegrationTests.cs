using System.Diagnostics;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Infrastructure.Outbox;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Api.IntegrationTests.Features.Outbox;

[Collection(IntegrationTestCollection.Name)]
public sealed class OutboxProcessorIntegrationTests
{
    private readonly HotelBookingApiFactory _factory;

    public OutboxProcessorIntegrationTests(HotelBookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Failing_Event_Moves_To_DeadLetter_After_MaxAttempts()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

            await outbox.EnqueueAsync(new DeadLetterTestEvent(marker), CancellationToken.None);
            await db.SaveChangesAsync();
        }

        var typeName = typeof(DeadLetterTestEvent).FullName!;

        var row = await WaitUntilAsync(
            async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await db.OutboxMessages
                    .AsNoTracking()
                    .Where(m => m.Type == typeName && m.Payload.Contains(marker))
                    .FirstOrDefaultAsync(m => m.Status == OutboxMessageStatus.DeadLetter);
            },
            TimeSpan.FromSeconds(15));

        row.Should().NotBeNull();
        row!.AttemptCount.Should().Be(2);
        row.LastError.Should().Contain("dead-letter-test-boom");
        row.ProcessedAtUtc.Should().BeNull();
    }

    private static async Task<T?> WaitUntilAsync<T>(Func<Task<T?>> probe, TimeSpan timeout)
        where T : class
    {
        var interval = TimeSpan.FromMilliseconds(200);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var value = await probe();
            if (value is not null) return value;
            await Task.Delay(interval);
        }
        return await probe();
    }
}   