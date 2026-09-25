using System.Text.Json;
using HotelBooking.Application.Features.Auth.Common;
using HotelBooking.Application.Features.Auth.ConfirmEmail;
using HotelBooking.Application.Features.Auth.Login;
using HotelBooking.Application.Features.Auth.Refresh;
using HotelBooking.Application.Features.Auth.Register;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Api.IntegrationTests.Features.Auth;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthFlowTests
{
    private readonly HotelBookingApiFactory _factory;

    public AuthFlowTests(HotelBookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Confirm_Login_Refresh_Logout_FullFlow_Works()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"user-{suffix}@test.com";
        var userName = $"user{suffix[..8]}";
        var password = "P@ssw0rd123!";

        var registerResp = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterCommand(email, userName, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var registered = await registerResp.Content.ReadFromJsonAsync<RegisterResponse>();
        registered!.Email.Should().Be(email);
        registered.UserName.Should().Be(userName);

        var token = await ReadConfirmationTokenAsync(email);
        token.Should().NotBeNullOrEmpty();

        var confirmResp = await client.PostAsJsonAsync(
            "/api/v1/auth/confirm-email",
            new ConfirmEmailCommand(token!));
        confirmResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResp = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginCommand(email, password));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loggedIn = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        loggedIn!.AccessToken.Should().NotBeNullOrEmpty();
        loggedIn.RefreshToken.Should().NotBeNullOrEmpty();

        var refreshResp = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshCommand(loggedIn.RefreshToken));
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResp.Content.ReadFromJsonAsync<RefreshResponse>();
        refreshed!.AccessToken.Should().NotBeNullOrEmpty();
        refreshed.RefreshToken.Should().NotBe(loggedIn.RefreshToken);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

        var logoutResp = await client.PostAsync("/api/v1/auth/logout", content: null);
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var replayResp = await client.PostAsync("/api/v1/auth/logout", content: null);
        replayResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WhenTokenReused_RevokesAllSessionsIncludingNewAccessToken()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"reuse-{suffix}@test.com";
        var userName = $"reuse{suffix[..8]}";
        var password = "P@ssw0rd123!";

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterCommand(email, userName, password));

        var token = await ReadConfirmationTokenAsync(email);
        token.Should().NotBeNullOrEmpty();

        await client.PostAsJsonAsync(
            "/api/v1/auth/confirm-email",
            new ConfirmEmailCommand(token!));

        var loginResp = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginCommand(email, password));
        var loggedIn = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        var refreshResp = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshCommand(loggedIn!.RefreshToken));
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResp.Content.ReadFromJsonAsync<RefreshResponse>();

        var reuseResp = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshCommand(loggedIn.RefreshToken));
        reuseResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshed!.AccessToken);

        var protectedResp = await client.PostAsync("/api/v1/auth/logout", content: null);
        protectedResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"dup-{suffix}@test.com";

        var first = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterCommand(email, $"dupA{suffix[..8]}", "P@ssw0rd123!"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterCommand(email, $"dupB{suffix[..8]}", "P@ssw0rd123!"));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"wrong-{suffix}@test.com";
        var userName = $"wrong{suffix[..8]}";

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterCommand(email, userName, "P@ssw0rd123!"));

        var loginResp = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginCommand(email, "wrong-password"));

        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string?> ReadConfirmationTokenAsync(string email)
    {
        var typeName = typeof(UserRegisteredEvent).FullName!;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (var i = 0; i < 50; i++)
        {
            var row = await db.OutboxMessages
                .AsNoTracking()
                .Where(m => m.Type == typeName && m.Payload.Contains(email))
                .OrderByDescending(m => m.OccurredAtUtc)
                .FirstOrDefaultAsync();
            if (row is not null)
            {
                using var doc = JsonDocument.Parse(row.Payload);
                return doc.RootElement.GetProperty("confirmationToken").GetString();
            }
            await Task.Delay(100);
        }
        return null;
    }
}