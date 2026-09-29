using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Hotels.GetDetails.Abstractions;

namespace HotelBooking.Application.Features.Hotels.GetDetails;

public sealed class GetHotelDetailsQueryHandler
    : IQueryHandler<GetHotelDetailsQuery, HotelDetailsDto>
{
    private readonly IHotelDetailsReader _reader;

    public GetHotelDetailsQueryHandler(IHotelDetailsReader reader) => _reader = reader;

    public async Task<Result<HotelDetailsDto>> Handle(
        GetHotelDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var hotel = await _reader.GetDetailsAsync(query, cancellationToken);

        return hotel is null
            ? Error.NotFound("Hotel.NotFound", $"Hotel {query.HotelId} was not found.")
            : hotel;
    }
}
