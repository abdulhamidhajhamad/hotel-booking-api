using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Cities.Common;

namespace HotelBooking.Application.Features.Admin.Cities.Update;

public sealed record UpdateCityCommand(
    Guid Id,
    string? Name = null,
    string? Country = null,
    string? PostalCode = null,
    string? Timezone = null) : ICommand<CityDetail>;