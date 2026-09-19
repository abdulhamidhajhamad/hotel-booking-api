using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Features.Admin.RoomImages.Common;

namespace HotelBooking.Application.Features.Admin.RoomImages.Upload;

public sealed record UploadRoomImagesCommand(
    Guid HotelId,
    Guid RoomId,
    IReadOnlyList<UploadImageFile> Files) : ICommand<IReadOnlyList<RoomImageDto>>;