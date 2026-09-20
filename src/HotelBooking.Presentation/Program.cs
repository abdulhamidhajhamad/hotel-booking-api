using Serilog;
using Serilog.Core;
using HotelBooking.Application;
using HotelBooking.Application.Abstractions;
using HotelBooking.Infrastructure;
using HotelBooking.Presentation.Common;
using HotelBooking.Presentation.Common.Logging;
using HotelBooking.Presentation.Common.Middleware;

DotNetEnv.Env.Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
        ?? throw new InvalidOperationException(
            "DB_CONNECTION missing - check your .env file at the solution root.");

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(connectionString);

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
    builder.Services.AddOpenApi();

    builder.Services.AddHealthChecks();

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
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