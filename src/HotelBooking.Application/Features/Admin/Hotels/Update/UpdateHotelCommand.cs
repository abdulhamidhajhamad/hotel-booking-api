using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.Update;

public sealed record UpdateHotelCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    int? StarRating = null,
    HotelCategory? Category = null,
    string? Address = null,
    double? Latitude = null,
    double? Longitude = null,
    Guid? CityId = null,
    string? OwnerName = null) : ICommand<HotelDetail>;