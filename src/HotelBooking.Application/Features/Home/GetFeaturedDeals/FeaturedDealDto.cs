namespace HotelBooking.Application.Features.Home.GetFeaturedDeals;

public sealed record FeaturedDealDto(
    Guid HotelId,
    string HotelName,
    string CityName,
    string Country,
    int StarRating,
    string? ThumbnailUrl,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    decimal DiscountPercentage);