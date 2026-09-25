using HotelBooking.Application.Abstractions;

namespace HotelBooking.Api.IntegrationTests.Infrastructure;

public sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(Guid userId) => Id = userId;

    public Guid? Id { get; }
    public string? Email => null;
    public bool IsAuthenticated => Id is not null;
    public string? Jti => null;
    public DateTimeOffset? AccessTokenExpiresAt => null;
}