using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomImages.Abstractions;
using HotelBooking.Application.Features.Admin.RoomImages.Common;
using HotelBooking.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.RoomImages.Upload;

public sealed class UploadRoomImagesCommandHandler
    : ICommandHandler<UploadRoomImagesCommand, IReadOnlyList<RoomImageDto>>
{
    private readonly IRoomImageRepository _images;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorage _storage;
    private readonly ILogger<UploadRoomImagesCommandHandler> _logger;

    public UploadRoomImagesCommandHandler(
        IRoomImageRepository images,
        IUnitOfWork unitOfWork,
        IImageStorage storage,
        ILogger<UploadRoomImagesCommandHandler> logger)
    {
        _images = images;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<RoomImageDto>>> Handle(
        UploadRoomImagesCommand command,
        CancellationToken cancellationToken)
    {
        var roomExists = await _images.RoomExistsAsync(command.RoomId, command.HotelId, cancellationToken);

        if (!roomExists)
            return RoomImageErrors.RoomNotFound(command.HotelId, command.RoomId);
        var items = new List<RoomImageDto>();
        var uploadedPublicIds = new List<string>();
        foreach (var file in command.Files)
        {
            var stored = await _storage.UploadAsync(
                file.Content,
                file.ContentType,
                $"hotels/{command.HotelId}/rooms/{command.RoomId}",
                cancellationToken);

            uploadedPublicIds.Add(stored.PublicId);

            var image = new RoomImage
            {
                RoomId = command.RoomId,
                Url = stored.Url,
                PublicId = stored.PublicId,
            };
            _images.Add(image);

            items.Add(new RoomImageDto(image.Id, image.Url));
        }
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var publicId in uploadedPublicIds)
            {
                try
                {
                    await _storage.DeleteAsync(publicId, CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx,
                        "Failed to delete orphaned upload {PublicId} after a failed image save.",
                        publicId);
                }
            }
            throw;
        }
        return Result<IReadOnlyList<RoomImageDto>>.Success(items);
    }
}
