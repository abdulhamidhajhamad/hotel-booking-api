using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Email;
using HotelBooking.Application.Abstractions.Invoicing;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Abstractions.Payments;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Domain.Identity;
using HotelBooking.Infrastructure.Email;
using HotelBooking.Application.Common.Options;
using HotelBooking.Infrastructure.Email.Options;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Identity.Options;
using HotelBooking.Infrastructure.Invoicing;
using HotelBooking.Infrastructure.Outbox;
using HotelBooking.Infrastructure.Payments;
using HotelBooking.Infrastructure.Payments.Options;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Storage;
using HotelBooking.Infrastructure.Storage.Options;

namespace HotelBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions,
        string redisConnection,
        CloudinaryOptions cloudinaryOptions,
        SmtpOptions smtpOptions,
        EmailConfirmationOptions emailConfirmationOptions,
        StripeOptions stripeOptions)
    {
        services.AddSingleton<OutboxSignal>();
        services.AddSingleton(new OutboxEventTypeRegistry(
            typeof(IIntegrationEvent).Assembly,
            typeof(IOutboxHandler<>).Assembly));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));
        services.AddSingleton<OutboxSignalInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options
                .UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                })
                .AddInterceptors(sp.GetRequiredService<OutboxSignalInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtOptions));

        services.AddScoped<IUserRegistrar, UserRegistrar>();
        services.AddScoped<IAdminUserCreator, AdminUserCreator>();
        services.AddScoped<IUserAuthenticator, UserAuthenticator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenIssuer, RefreshTokenIssuer>();
        services.AddScoped<IRefreshTokenRotator, RefreshTokenRotator>();
        services.AddScoped<IRefreshTokenRevoker, RefreshTokenRevoker>();
        services.AddScoped<IEmailConfirmationTokenIssuer, EmailConfirmationTokenIssuer>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<IJtiBlacklist, RedisJtiBlacklist>();

        services.AddSingleton(TimeProvider.System);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero,
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst(
                            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                        if (string.IsNullOrEmpty(jti)) return;

                        var blacklist = context.HttpContext.RequestServices
                            .GetRequiredService<IJtiBlacklist>();

                        if (await blacklist.IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
                            context.Fail("Token has been revoked.");
                    }
                };
            });

        services.AddAuthorization();

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(cloudinaryOptions));
        services.AddScoped<IImageStorage, CloudinaryImageStorage>();

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(smtpOptions));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(emailConfirmationOptions));
        services.AddScoped<IEmailSender, MailKitEmailSender>();

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(stripeOptions));
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddSingleton<IInvoiceRenderer, QuestPdfInvoiceRenderer>();

        services.AddScoped<OutboxDispatcher>();
        services.AddScoped<IOutboxAdmin, OutboxAdmin>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}