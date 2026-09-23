using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Hotels.Search;

public sealed record HotelSearchResultDto(
    Guid HotelId,
    string HotelName,
    string CityName,
    string Country,
    int StarRating,
    HotelCategory Category,
    string? Description,
    string? ThumbnailUrl,
    decimal OriginalPricePerNight,
    decimal DiscountedPricePerNight);