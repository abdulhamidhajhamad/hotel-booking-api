using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomImages.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.RoomImages.Upload;

public sealed class UploadRoomImagesCommandHandler
    : ICommandHandler<UploadRoomImagesCommand, IReadOnlyList<RoomImageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorage _storage;
    private readonly ILogger<UploadRoomImagesCommandHandler> _logger;

    public UploadRoomImagesCommandHandler(
        IApplicationDbContext db,
        IImageStorage storage,
        ILogger<UploadRoomImagesCommandHandler> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
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
            await _db.RoomImages.AddAsync(image, cancellationToken);

            items.Add(new RoomImageDto(image.Id, image.Url));
        }
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
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