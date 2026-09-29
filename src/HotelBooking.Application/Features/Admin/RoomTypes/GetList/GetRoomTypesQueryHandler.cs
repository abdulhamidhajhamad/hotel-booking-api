using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;

namespace HotelBooking.Application.Features.Admin.RoomTypes.GetList;

public sealed class GetRoomTypesQueryHandler
    : IQueryHandler<GetRoomTypesQuery, IReadOnlyList<RoomTypeDto>>
{
    private readonly IRoomTypeReader _roomTypes;

    public GetRoomTypesQueryHandler(IRoomTypeReader roomTypes) => _roomTypes = roomTypes;

    public async Task<Result<IReadOnlyList<RoomTypeDto>>> Handle(
        GetRoomTypesQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _roomTypes.GetAllAsync(cancellationToken);

        return Result<IReadOnlyList<RoomTypeDto>>.Success(items);
    }
}
