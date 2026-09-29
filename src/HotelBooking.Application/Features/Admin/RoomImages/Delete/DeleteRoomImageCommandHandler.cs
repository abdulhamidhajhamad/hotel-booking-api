using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomImages.Abstractions;
using HotelBooking.Application.Features.Admin.RoomImages.Common;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.RoomImages.Delete;

public sealed class DeleteRoomImageCommandHandler
    : ICommandHandler<DeleteRoomImageCommand>
{
    private readonly IRoomImageRepository _images;
    private readonly IImageStorage _storage;
    private readonly ILogger<DeleteRoomImageCommandHandler> _logger;

    public DeleteRoomImageCommandHandler(
        IRoomImageRepository images,
        IImageStorage storage,
        ILogger<DeleteRoomImageCommandHandler> logger)
    {
        _images = images;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteRoomImageCommand command,
        CancellationToken cancellationToken)
    {
        var image = await _images.GetByIdAsync(command.ImageId, command.RoomId, command.HotelId, cancellationToken);

        if (image is null)
            return Result.Failure(RoomImageErrors.NotFound(command.ImageId));

        _images.Remove(image);

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
