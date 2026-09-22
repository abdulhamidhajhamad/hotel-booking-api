using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomImages.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomImages.Upload;

public sealed class UploadRoomImagesCommandHandler
    : ICommandHandler<UploadRoomImagesCommand, IReadOnlyList<RoomImageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorage _storage;

    public UploadRoomImagesCommandHandler(
        IApplicationDbContext db,
        IImageStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Result<IReadOnlyList<RoomImageDto>>> Handle(
        UploadRoomImagesCommand command,
        CancellationToken cancellationToken)
    {
        var roomExists = await _db.Rooms.AnyAsync(
            r => r.Id == command.RoomId && r.HotelId == command.HotelId,
            cancellationToken);

        if (!roomExists)
            return RoomImageErrors.RoomNotFound(command.HotelId, command.RoomId);

        var items = new List<RoomImageDto>();

        foreach (var file in command.Files)
        {
            var stored = await _storage.UploadAsync(
                file.Content,
                file.ContentType,
                $"hotels/{command.HotelId}/rooms/{command.RoomId}",
                cancellationToken);

            var image = new RoomImage
            {
                RoomId = command.RoomId,
                Url = stored.Url,
                PublicId = stored.PublicId,
            };
            await _db.RoomImages.AddAsync(image, cancellationToken);

            items.Add(new RoomImageDto(image.Id, image.Url));
        }

        return Result<IReadOnlyList<RoomImageDto>>.Success(items);
    }
}