using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.HotelImages.SetPrimary;

public sealed record SetPrimaryHotelImageCommand(
    Guid HotelId,
    Guid ImageId) : ICommand;