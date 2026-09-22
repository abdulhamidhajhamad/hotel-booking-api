using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Amenities.Delete;

public sealed record DeleteAmenityCommand(Guid Id) : ICommand;