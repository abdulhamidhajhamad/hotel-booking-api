using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomImages.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.RoomImages.Delete;

public sealed class DeleteRoomImageCommandHandler
    : ICommandHandler<DeleteRoomImageCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorage _storage;
    private readonly ILogger<DeleteRoomImageCommandHandler> _logger;

    public DeleteRoomImageCommandHandler(
        IApplicationDbContext db,
        IImageStorage storage,
        ILogger<DeleteRoomImageCommandHandler> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteRoomImageCommand command,
        CancellationToken cancellationToken)
    {
        var image = await _db.RoomImages.FirstOrDefaultAsync(
            i => i.Id == command.ImageId
                 && i.RoomId == command.RoomId
                 && i.Room.HotelId == command.HotelId,
            cancellationToken);

        if (image is null)
            return Result.Failure(RoomImageErrors.NotFound(command.ImageId));

        _db.RoomImages.Remove(image);

        try
        {
            await _storage.DeleteAsync(image.PublicId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Cloudinary delete failed for RoomImage {ImageId} publicId '{PublicId}'. Orphan will be cleaned later.",
                image.Id, image.PublicId);
        }

        return Result.Success();
    }
}