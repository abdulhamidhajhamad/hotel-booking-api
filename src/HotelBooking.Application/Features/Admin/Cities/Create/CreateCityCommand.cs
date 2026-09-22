using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Cities.Common;

namespace HotelBooking.Application.Features.Admin.Cities.Create;

public sealed record CreateCityCommand(
    string Name,
    string Country,
    string? PostalCode,
    string Timezone) : ICommand<CityDetail>;