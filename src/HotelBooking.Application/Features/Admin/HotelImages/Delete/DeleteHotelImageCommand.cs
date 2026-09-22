using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.HotelImages.Delete;

public sealed record DeleteHotelImageCommand(
    Guid HotelId,
    Guid ImageId) : ICommand;