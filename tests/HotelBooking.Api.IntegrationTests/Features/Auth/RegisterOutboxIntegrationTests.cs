using System.Diagnostics;
using HotelBooking.Application.Features.Auth.Common;
using HotelBooking.Application.Features.Auth.Register;
using HotelBooking.Infrastructure.Outbox;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Api.IntegrationTests.Features.Auth;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterOutboxIntegrationTests
{
    private readonly HotelBookingApiFactory _factory;

    public RegisterOutboxIntegrationTests(HotelBookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_EnqueuesAndDispatchesConfirmationEmail()
    {
        _factory.Emails.Clear();

        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"outbox-{suffix}@test.com";
        var userName = $"ob{suffix[..8]}";

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterCommand(email, userName, "P@ssw0rd123!"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sent = await WaitForAsync(
            () => _factory.Emails.Sent.FirstOrDefault(m => m.ToEmail == email),
            TimeSpan.FromSeconds(10));

        sent.Should().NotBeNull();
        sent!.ToName.Should().Be(userName);
        sent.Subject.Should().Be("Confirm your Hotel Booking account");
        sent.HtmlBody.Should().Contain("Confirm my email");
        sent.PlainTextBody.Should().NotBeNullOrEmpty();

        var typeName = typeof(UserRegisteredEvent).FullName!;
        var row = await WaitForAsync(
            async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await db.OutboxMessages
                    .AsNoTracking()
                    .Where(m => m.Type == typeName && m.Payload.Contains(email))
                    .OrderByDescending(m => m.OccurredAtUtc)
                    .FirstOrDefaultAsync(m => m.Status == OutboxMessageStatus.Processed);
            },
            TimeSpan.FromSeconds(5));

        row.Should().NotBeNull();
        row!.ProcessedAtUtc.Should().NotBeNull();
        row.LastError.Should().BeNull();
        row.AttemptCount.Should().Be(0);
    }

    private static async Task<T?> WaitForAsync<T>(Func<T?> probe, TimeSpan timeout)
        where T : class
    {
        var interval = TimeSpan.FromMilliseconds(100);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var value = probe();
            if (value is not null) return value;
            await Task.Delay(interval);
        }
        return probe();
    }

    private static async Task<T?> WaitForAsync<T>(Func<Task<T?>> probe, TimeSpan timeout)
        where T : class
    {
        var interval = TimeSpan.FromMilliseconds(100);
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