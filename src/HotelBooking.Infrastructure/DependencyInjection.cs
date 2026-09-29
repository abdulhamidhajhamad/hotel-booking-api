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
using HotelBooking.Infrastructure.Bookings;
using HotelBooking.Application.Features.Bookings.Invoice.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.Invoices;
using HotelBooking.Application.Features.Reviews.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.Reviews;
using HotelBooking.Application.Features.Cities.GetTrending.Abstractions;
using HotelBooking.Application.Features.Hotels.GetDetails.Abstractions;
using HotelBooking.Application.Features.Hotels.GetFeaturedDeals.Abstractions;
using HotelBooking.Application.Features.Hotels.RecentlyVisited.Abstractions;
using HotelBooking.Application.Features.Hotels.Search.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.TrendingDestinations;
using HotelBooking.Infrastructure.Persistence.Repositories.HotelDetails;
using HotelBooking.Infrastructure.Persistence.Repositories.FeaturedDeals;
using HotelBooking.Infrastructure.Persistence.Repositories.RecentlyVisitedHotels;
using HotelBooking.Infrastructure.Persistence.Repositories.HotelSearch;
using HotelBooking.Application.Features.Auth.Email.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.EmailConfirmation;
using HotelBooking.Application.Features.Admin.CityImages.Abstractions;
using HotelBooking.Application.Features.Admin.HotelImages.Abstractions;
using HotelBooking.Application.Features.Admin.RoomImages.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.CityImages;
using HotelBooking.Infrastructure.Persistence.Repositories.HotelImages;
using HotelBooking.Infrastructure.Persistence.Repositories.RoomImages;
using HotelBooking.Application.Features.Admin.Rooms.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.Rooms;
using HotelBooking.Application.Features.Admin.Hotels.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.Hotels;
using HotelBooking.Application.Features.Admin.Discounts.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.Discounts;
using HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;
using HotelBooking.Application.Features.Admin.Cities.Abstractions;
using HotelBooking.Infrastructure.Persistence.Repositories.Amenities;
using HotelBooking.Infrastructure.Persistence.Repositories.Cities;
using HotelBooking.Infrastructure.Persistence.Repositories.RoomTypes;
using HotelBooking.Application.Features.Admin.Amenities.Abstractions;
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

        services.AddScoped<IInvoiceReader, InvoiceReader>();

        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewReader, ReviewReader>();

        services.AddScoped<ITrendingDestinationsReader, TrendingDestinationsReader>();
        services.AddScoped<IHotelDetailsReader, HotelDetailsReader>();
        services.AddScoped<IFeaturedDealsReader, FeaturedDealsReader>();
        services.AddScoped<IRecentlyVisitedHotelsReader, RecentlyVisitedHotelsReader>();
        services.AddScoped<IHotelSearchReader, HotelSearchReader>();

        services.AddScoped<IEmailConfirmationRepository, EmailConfirmationRepository>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ICityImageRepository, CityImageRepository>();
        services.AddScoped<IHotelImageRepository, HotelImageRepository>();
        services.AddScoped<IRoomImageRepository, RoomImageRepository>();

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IRoomReader, RoomReader>();

        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IHotelReader, HotelReader>();

        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IDiscountReader, DiscountReader>();

        services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
        services.AddScoped<IRoomTypeReader, RoomTypeReader>();

        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<ICityReader, CityReader>();

        services.AddScoped<IAmenityRepository, AmenityRepository>();
        services.AddScoped<IAmenityReader, AmenityReader>();

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
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ExpiredHoldSweeperOptions()));
        services.AddHostedService<ExpiredHoldSweeper>();
        return services;
    }
}