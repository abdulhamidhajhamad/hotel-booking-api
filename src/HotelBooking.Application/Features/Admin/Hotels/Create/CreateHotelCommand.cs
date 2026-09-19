using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.Create;

public sealed record CreateHotelCommand(
    string Name,
    string? Description,
    int StarRating,
    HotelCategory Category,
    string Address,
    double? Latitude,
    double? Longitude,
    Guid CityId,
    Guid? OwnerId) : ICommand<HotelDetail>;