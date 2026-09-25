using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.CityImages.Delete;

public sealed record DeleteCityImageCommand(
    Guid CityId,
    Guid ImageId) : ICommand;