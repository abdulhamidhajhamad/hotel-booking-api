using HotelBooking.Application;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Options;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Email.Options;
using HotelBooking.Infrastructure.Identity.Options;
using HotelBooking.Infrastructure.Payments.Options;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Storage.Options;
using HotelBooking.Presentation.Common;
using HotelBooking.Presentation.Common.Logging;
using HotelBooking.Presentation.Common.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using System.Threading.RateLimiting;

DotNetEnv.Env.TraversePath().Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
        ?? throw new InvalidOperationException("DB_CONNECTION missing - check your .env.");

    var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
        ?? throw new InvalidOperationException("REDIS_CONNECTION missing - check your .env.");

    var jwtOptions = new JwtOptions
    {
        Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? throw new InvalidOperationException("JWT_ISSUER missing - check your .env."),
        Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? throw new InvalidOperationException("JWT_AUDIENCE missing - check your .env."),
        SigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
            ?? throw new InvalidOperationException("JWT_SIGNING_KEY missing - check your .env."),
        AccessTokenMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_MINUTES") ?? "15"),
        RefreshTokenMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_MINUTES") ?? "30"),
    };

    var cloudinaryOptions = new CloudinaryOptions
    {
        CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? throw new InvalidOperationException("CLOUDINARY_CLOUD_NAME missing - check your .env."),
        ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? throw new InvalidOperationException("CLOUDINARY_API_KEY missing - check your .env."),
        ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? throw new InvalidOperationException("CLOUDINARY_API_SECRET missing - check your .env."),
        DefaultFolder = Environment.GetEnvironmentVariable("CLOUDINARY_FOLDER") ?? "hotel-booking",
    };

    var smtpOptions = new SmtpOptions
    {
        Host = Environment.GetEnvironmentVariable("SMTP_HOST")
            ?? throw new InvalidOperationException("SMTP_HOST missing - check your .env."),
        Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")
            ?? throw new InvalidOperationException("SMTP_PORT missing - check your .env.")),
        Username = Environment.GetEnvironmentVariable("SMTP_USERNAME"),
        Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
        UseSsl = bool.Parse(Environment.GetEnvironmentVariable("SMTP_USE_SSL") ?? "false"),
        FromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")
            ?? throw new InvalidOperationException("SMTP_FROM_EMAIL missing - check your .env."),
        FromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "Hotel Booking",
    };

    var emailConfirmationOptions = new EmailConfirmationOptions
    {
        TokenLifetimeHours = int.Parse(Environment.GetEnvironmentVariable("EMAIL_CONFIRM_TOKEN_LIFETIME_HOURS") ?? "24"),
        TokenByteLength = int.Parse(Environment.GetEnvironmentVariable("EMAIL_CONFIRM_TOKEN_BYTE_LENGTH") ?? "32"),
        ConfirmUrlTemplate = Environment.GetEnvironmentVariable("EMAIL_CONFIRM_URL_TEMPLATE")
            ?? throw new InvalidOperationException("EMAIL_CONFIRM_URL_TEMPLATE missing - check your .env."),
        ResendCooldownSeconds = int.Parse(Environment.GetEnvironmentVariable("EMAIL_CONFIRM_RESEND_COOLDOWN_SECONDS") ?? "60"),
    };

    var stripeOptions = new StripeOptions
    {
        SecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY missing - check your .env."),
        Currency = Environment.GetEnvironmentVariable("STRIPE_CURRENCY") ?? "USD",
    };

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(connectionString, jwtOptions, redisConnection, cloudinaryOptions, smtpOptions, emailConfirmationOptions, stripeOptions);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    builder.Services.AddSingleton<ILogEventEnricher, ApplicationEnricher>();
    builder.Services.AddSingleton<ILogEventEnricher, CorrelationIdEnricher>();
    builder.Services.AddSingleton<ILogEventEnricher, RequestContextEnricher>();
    builder.Services.AddSingleton<ILogEventEnricher, UserContextEnricher>();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("auth-register", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: PartitionKeyFor(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                }));

        options.AddPolicy("auth-login", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: PartitionKeyFor(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                }));

        options.AddPolicy("auth-confirm", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: PartitionKeyFor(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                }));

        options.AddPolicy("auth-resend", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: PartitionKeyFor(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                }));

        static string PartitionKeyFor(HttpContext context)
            => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "HotelBooking API", Version = "v1" });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste your JWT access token (no \"Bearer\" prefix)."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            }] = Array.Empty<string>()
        });
    });

    builder.Services.AddHealthChecks();
    builder.Services.AddMemoryCache();
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await HotelBooking.Presentation.Common.AdminSeeder.SeedAsync(scope.ServiceProvider);
    }

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelBooking API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
public partial class Program;
