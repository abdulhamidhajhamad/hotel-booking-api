using HotelBooking.Api.IntegrationTests.Features.Outbox;
using HotelBooking.Application.Abstractions.Email;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Infrastructure.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Microsoft.AspNetCore.TestHost;
using HotelBooking.Infrastructure.Bookings;
namespace HotelBooking.Api.IntegrationTests.Infrastructure;

public sealed class HotelBookingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_strong_Passw0rd!")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    public FakeEmailSender Emails => Services.GetRequiredService<FakeEmailSender>();

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

        Environment.SetEnvironmentVariable("CLOUDINARY_CLOUD_NAME", "test-cloud");
        Environment.SetEnvironmentVariable("CLOUDINARY_API_KEY", "test-api-key");
        Environment.SetEnvironmentVariable("CLOUDINARY_API_SECRET", "test-api-secret");

        Environment.SetEnvironmentVariable("SMTP_HOST", "localhost");
        Environment.SetEnvironmentVariable("SMTP_PORT", "1025");
        Environment.SetEnvironmentVariable("SMTP_FROM_EMAIL", "no-reply@hotelbooking.test");

        Environment.SetEnvironmentVariable(
            "EMAIL_CONFIRM_URL_TEMPLATE",
            "http://localhost:3000/confirm-email#token={token}");

        Environment.SetEnvironmentVariable("STRIPE_SECRET_KEY", "sk_test_dummy");

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var senderDescriptor = services.Single(d => d.ServiceType == typeof(IEmailSender));
            services.Remove(senderDescriptor);
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<FakeEmailSender>());

            var paymentGatewayDescriptor = services.Single(d => d.ServiceType == typeof(HotelBooking.Application.Abstractions.Payments.IPaymentGateway));
            services.Remove(paymentGatewayDescriptor);
            services.AddScoped<HotelBooking.Application.Abstractions.Payments.IPaymentGateway, AlwaysSucceedsPaymentGateway>();

            var outboxOptionsDescriptor = services.Single(d => d.ServiceType == typeof(IOptions<OutboxOptions>));
            services.Remove(outboxOptionsDescriptor);
            services.AddSingleton<IOptions<OutboxOptions>>(Options.Create(new OutboxOptions
            {
                BatchSize = 20,
                PollFallbackSeconds = 1,
                MaxAttempts = 2,
                BackoffBaseSeconds = 1,
                BackoffMaxSeconds = 2,
            }));
            var sweeperOptionsDescriptor = services.Single(d => d.ServiceType == typeof(IOptions<ExpiredHoldSweeperOptions>));
            services.Remove(sweeperOptionsDescriptor);
            services.AddSingleton<IOptions<ExpiredHoldSweeperOptions>>(Options.Create(new ExpiredHoldSweeperOptions
            {
                SweepIntervalSeconds = 1,
            }));
            services.AddSingleton<IOutboxHandler<DeadLetterTestEvent>, ThrowingDeadLetterHandler>();

            var registryDescriptor = services.Single(d => d.ServiceType == typeof(OutboxEventTypeRegistry));
            services.Remove(registryDescriptor);
            services.AddSingleton(new OutboxEventTypeRegistry(
                typeof(IIntegrationEvent).Assembly,
                typeof(IOutboxHandler<>).Assembly,
                typeof(HotelBookingApiFactory).Assembly));
        });
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
