using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.HotelImages.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.HotelImages.Delete;

public sealed class DeleteHotelImageCommandHandler
    : ICommandHandler<DeleteHotelImageCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorage _storage;
    private readonly ILogger<DeleteHotelImageCommandHandler> _logger;

    public DeleteHotelImageCommandHandler(
        IApplicationDbContext db,
        IImageStorage storage,
        ILogger<DeleteHotelImageCommandHandler> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteHotelImageCommand command,
        CancellationToken cancellationToken)
    {
        var image = await _db.HotelImages.FirstOrDefaultAsync(
            i => i.Id == command.ImageId && i.HotelId == command.HotelId,
            cancellationToken);

        if (image is null)
            return Result.Failure(HotelImageErrors.NotFound(command.ImageId));

        var wasPrimary = image.IsPrimary;
        _db.HotelImages.Remove(image);

        if (wasPrimary)
        {
            var next = await _db.HotelImages
                .Where(i => i.HotelId == command.HotelId && i.Id != command.ImageId)
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (next is not null) next.IsPrimary = true;
        }

        try
        {
            await _storage.DeleteAsync(image.PublicId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Cloudinary delete failed for HotelImage {ImageId} publicId '{PublicId}'. Orphan will be cleaned later.",
                image.Id, image.PublicId);
        }

        return Result.Success();
    }
}