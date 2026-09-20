using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Auth.Login;
using HotelBooking.Application.Features.Auth.Logout;
using HotelBooking.Application.Features.Auth.LogoutAll;
using HotelBooking.Application.Features.Auth.Refresh;
using HotelBooking.Application.Features.Auth.Register;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<
            ICommandHandler<RegisterCommand, RegisterResponse>,
            RegisterCommandHandler>();

        services.AddScoped<
            ICommandHandler<LoginCommand, LoginResponse>,
            LoginCommandHandler>();

        services.AddScoped<
            ICommandHandler<RefreshCommand, RefreshResponse>,
            RefreshCommandHandler>();

        services.AddScoped<ICommandHandler<LogoutCommand>, LogoutCommandHandler>();

        services.AddScoped<ICommandHandler<LogoutAllCommand>, LogoutAllCommandHandler>();

        return services;
    }
}