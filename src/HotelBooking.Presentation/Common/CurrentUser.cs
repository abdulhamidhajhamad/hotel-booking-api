using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelBooking.Application.Abstractions;

namespace HotelBooking.Presentation.Common;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? Id
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;


    public string? Jti => User?.FindFirstValue(JwtRegisteredClaimNames.Jti);

    public DateTimeOffset? AccessTokenExpiresAt
    {
        get
        {
            var exp = User?.FindFirstValue(JwtRegisteredClaimNames.Exp);
            if (long.TryParse(exp, out var seconds))
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            return null;
        }
    }
}