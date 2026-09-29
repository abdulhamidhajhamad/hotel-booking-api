using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Abstractions;
using HotelBooking.Application.Features.Admin.Amenities.Common;

namespace HotelBooking.Application.Features.Admin.Amenities.GetList;

public sealed class GetAmenitiesQueryHandler
    : IQueryHandler<GetAmenitiesQuery, IReadOnlyList<AmenityDto>>
{
    private readonly IAmenityReader _amenities;

    public GetAmenitiesQueryHandler(IAmenityReader amenities) => _amenities = amenities;

    public async Task<Result<IReadOnlyList<AmenityDto>>> Handle(
        GetAmenitiesQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _amenities.GetAllAsync(cancellationToken);

        return Result<IReadOnlyList<AmenityDto>>.Success(items);
    }
}
