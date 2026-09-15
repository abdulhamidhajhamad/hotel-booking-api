using HotelBooking.Application;
using HotelBooking.Application.Abstractions;
using HotelBooking.Infrastructure;
using HotelBooking.Presentation.Common;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                       ?? throw new InvalidOperationException(
                           "DB_CONNECTION missing - check your .env file at the solution root.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

var app = builder.Build();

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