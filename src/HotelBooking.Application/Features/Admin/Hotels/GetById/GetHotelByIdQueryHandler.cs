using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Abstractions;
using HotelBooking.Application.Features.Admin.Hotels.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.GetById;

public sealed class GetHotelByIdQueryHandler
    : IQueryHandler<GetHotelByIdQuery, HotelDetail>
{
    private readonly IHotelReader _hotels;

    public GetHotelByIdQueryHandler(IHotelReader hotels) => _hotels = hotels;

    public async Task<Result<HotelDetail>> Handle(
        GetHotelByIdQuery query,
        CancellationToken cancellationToken)
    {
        var hotel = await _hotels.GetDetailAsync(query.Id, cancellationToken);

        return hotel is null
            ? HotelErrors.NotFound(query.Id)
            : hotel;
    }
}
