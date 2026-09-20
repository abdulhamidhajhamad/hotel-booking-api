using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace HotelBooking.Api.IntegrationTests.Infrastructure;

public sealed class HotelBookingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_strong_Passw0rd!")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        await _redis.StartAsync();

        Environment.SetEnvironmentVariable(
            "DB_CONNECTION",
            _db.GetConnectionString() + ";TrustServerCertificate=True;MultipleActiveResultSets=true");
        Environment.SetEnvironmentVariable(
            "REDIS_CONNECTION",
            _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("JWT_ISSUER", "HotelBooking.Tests");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "HotelBooking.Tests.Client");
        Environment.SetEnvironmentVariable(
            "JWT_SIGNING_KEY",
            "integration-test-signing-key-that-is-well-over-32-bytes-long-for-hs256");
        Environment.SetEnvironmentVariable("JWT_ACCESS_MINUTES", "15");
        Environment.SetEnvironmentVariable("JWT_REFRESH_MINUTES", "30");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}