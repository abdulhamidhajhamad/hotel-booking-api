using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Cities.Common;

namespace HotelBooking.Application.Features.Admin.Cities.GetById;

public sealed record GetCityByIdQuery(Guid Id) : IQuery<CityDetail>;