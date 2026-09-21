using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Domain.Identity;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Identity.Options;
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
        CloudinaryOptions cloudinaryOptions)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

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

        return services;
    }
}