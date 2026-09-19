using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Amenities.Common;

namespace HotelBooking.Application.Features.Admin.Amenities.GetList;

public sealed record GetAmenitiesQuery : IQuery<IReadOnlyList<AmenityDto>>;