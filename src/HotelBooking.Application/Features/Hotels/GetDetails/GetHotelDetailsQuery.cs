using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Hotels.GetDetails;

public sealed record GetHotelDetailsQuery(
    Guid HotelId,
    DateOnly? CheckIn = null,
    DateOnly? CheckOut = null) : IQuery<HotelDetailsDto>;