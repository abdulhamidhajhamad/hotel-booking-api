using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using HotelBooking.Application.Common.Messaging;
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

        return services;
    }
}