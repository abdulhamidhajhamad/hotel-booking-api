using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Amenities.Common;

namespace HotelBooking.Application.Features.Admin.Amenities.Create;

public sealed record CreateAmenityCommand(
    string Name,
    string? Icon) : ICommand<AmenityDto>;