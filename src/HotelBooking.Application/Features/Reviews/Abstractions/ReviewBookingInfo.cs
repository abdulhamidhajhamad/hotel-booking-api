using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Reviews.Abstractions;

public sealed record ReviewBookingInfo(Guid OwnerId, Guid HotelId, BookingStatus Status, DateOnly CheckOutDate);
