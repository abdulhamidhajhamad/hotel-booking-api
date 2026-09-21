using HotelBooking.Application.Features.Auth.Login;
using HotelBooking.Application.Features.Auth.Refresh;
using HotelBooking.Application.Features.Auth.Register;

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
    public async Task Register_Login_Refresh_Logout_FullFlow_Works()
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

        var reuseResp = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshCommand(loggedIn.RefreshToken));
        reuseResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

        var logoutResp = await client.PostAsync("/api/v1/auth/logout", content: null);
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var replayResp = await client.PostAsync("/api/v1/auth/logout", content: null);
        replayResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
}