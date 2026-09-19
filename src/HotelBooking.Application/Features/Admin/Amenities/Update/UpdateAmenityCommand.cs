using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Amenities.Common;

namespace HotelBooking.Application.Features.Admin.Amenities.Update;

public sealed record UpdateAmenityCommand(
    Guid Id,
    string? Name = null,
    string? Icon = null) : ICommand<AmenityDto>;