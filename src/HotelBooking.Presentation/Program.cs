using HotelBooking.Application;
using HotelBooking.Application.Abstractions;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Identity.Options;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Storage.Options;
using HotelBooking.Presentation.Common;
using HotelBooking.Presentation.Common.Logging;
using HotelBooking.Presentation.Common.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;

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

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(connectionString, jwtOptions, redisConnection, cloudinaryOptions);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    builder.Services.AddSingleton<ILogEventEnricher, ApplicationEnricher>();
    builder.Services.AddSingleton<ILogEventEnricher, CorrelationIdEnricher>();
    builder.Services.AddSingleton<ILogEventEnricher, RequestContextEnricher>();
    builder.Services.AddSingleton<ILogEventEnricher, UserContextEnricher>();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

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