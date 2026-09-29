using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.HotelImages.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.HotelImages.Upload;

public sealed class UploadHotelImagesCommandHandler
    : ICommandHandler<UploadHotelImagesCommand, IReadOnlyList<HotelImageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorage _storage;
    private readonly ILogger<UploadHotelImagesCommandHandler> _logger;

    public UploadHotelImagesCommandHandler(
        IApplicationDbContext db,
        IImageStorage storage,
        ILogger<UploadHotelImagesCommandHandler> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<HotelImageDto>>> Handle(
        UploadHotelImagesCommand command,
        CancellationToken cancellationToken)
    {
        var hotelExists = await _db.Hotels
            .AnyAsync(h => h.Id == command.HotelId, cancellationToken);
        if (!hotelExists)
            return HotelImageErrors.HotelNotFound(command.HotelId);

        var hasPrimary = await _db.HotelImages
            .AnyAsync(i => i.HotelId == command.HotelId && i.IsPrimary, cancellationToken);

        var items = new List<HotelImageDto>();
        var uploadedPublicIds = new List<string>();
        var isFirstInBatch = true;

        foreach (var file in command.Files)
        {
            var stored = await _storage.UploadAsync(
                file.Content,
                file.ContentType,
                $"hotels/{command.HotelId}",
                cancellationToken);

            uploadedPublicIds.Add(stored.PublicId);

            var makePrimary = !hasPrimary && isFirstInBatch;

            var image = new HotelImage
            {
                HotelId = command.HotelId,
                Url = stored.Url,
                PublicId = stored.PublicId,
                IsPrimary = makePrimary,
            };
            await _db.HotelImages.AddAsync(image, cancellationToken);
            if (makePrimary) hasPrimary = true;
            isFirstInBatch = false;

            items.Add(new HotelImageDto(image.Id, image.Url, image.IsPrimary));
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
        return Result<IReadOnlyList<HotelImageDto>>.Success(items);
    }
}