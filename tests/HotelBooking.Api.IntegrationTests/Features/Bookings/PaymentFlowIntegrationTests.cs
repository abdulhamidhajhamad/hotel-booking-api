using System.Diagnostics;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Application.Features.Bookings.Create;
using HotelBooking.Application.Features.Bookings.Pay;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Identity;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Api.IntegrationTests.Features.Bookings;

[Collection(IntegrationTestCollection.Name)]
public sealed class PaymentFlowIntegrationTests
{
    private readonly HotelBookingApiFactory _factory;

    public PaymentFlowIntegrationTests(HotelBookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBooking_ThenPay_ConfirmsBookingAndEmailsInvoice()
    {
        _factory.Emails.Clear();

        var (userEmail, accessToken, roomId) = await SeedUserRoomAndTokenAsync(pricePerNight: 100m);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var checkIn = new DateOnly(2030, 6, 1);
        var checkOut = checkIn.AddDays(2);
        var idempotencyKey = Guid.NewGuid().ToString("N");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var createResp = await client.PostAsJsonAsync(
            "/api/v1/bookings",
            new CreateBookingCommand(
                idempotencyKey,
                "Late check-in please",
                new[] { new CreateBookingRoomItem(roomId, checkIn, checkOut, Adults: 2, Children: 0) }));

        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<CreateBookingResult>();
        created!.TotalPrice.Should().Be(200m);
        created.ConfirmationNumber.Should().NotBeNullOrEmpty();
        created.HoldExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        var payResp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{created.BookingGroupId}/payment",
            new PayBookingCommand(created.BookingGroupId, "pm_card_visa"));

        payResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var paid = await payResp.Content.ReadFromJsonAsync<PayBookingResult>();
        paid!.PaymentStatus.Should().Be("Succeeded");
        paid.TotalPrice.Should().Be(200m);
        paid.ConfirmationNumber.Should().Be(created.ConfirmationNumber);

        var invoiceEmail = await WaitForAsync(
            () => _factory.Emails.Sent.FirstOrDefault(m =>
                m.ToEmail == userEmail && m.Subject.Contains(created.ConfirmationNumber)),
            TimeSpan.FromSeconds(10));

        invoiceEmail.Should().NotBeNull();
        invoiceEmail!.Attachments.Should().ContainSingle(a => a.ContentType == "application/pdf");
    }

    [Fact]
    public async Task Pay_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{Guid.NewGuid()}/payment",
            new PayBookingCommand(Guid.NewGuid(), "pm_card_visa"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string Email, string AccessToken, Guid RoomId)> SeedUserRoomAndTokenAsync(decimal pricePerNight)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        var email = $"pay-{Guid.NewGuid():N}@test.local";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
        user.NormalizedUserName = user.UserName!.ToUpperInvariant();
        user.NormalizedEmail = user.Email!.ToUpperInvariant();

        var city = new City { Name = $"City-{Guid.NewGuid():N}", Country = "TL", Timezone = "UTC" };
        var hotel = new Hotel { Name = $"Hotel-{Guid.NewGuid():N}", Address = "1 Test Street", StarRating = 4, City = city };
        var roomType = new RoomType { Name = $"Type-{Guid.NewGuid():N}" };
        var room = new Room
        {
            Hotel = hotel,
            RoomType = roomType,
            Number = "101",
            AdultsCapacity = 2,
            ChildrenCapacity = 1,
            PricePerNight = pricePerNight,
            IsActive = true,
        };

        db.Users.Add(user);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var jwt = tokens.Generate(user.Id, email, Array.Empty<string>());
        return (email, jwt.AccessToken, room.Id);
    }

    private static async Task<T?> WaitForAsync<T>(Func<T?> probe, TimeSpan timeout)
        where T : class
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var value = probe();
            if (value is not null) return value;
            await Task.Delay(100);
        }
        return probe();
    }
}
