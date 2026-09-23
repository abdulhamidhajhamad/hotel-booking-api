using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.CityImages.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.CityImages.Delete;

public sealed class DeleteCityImageCommandHandler
    : ICommandHandler<DeleteCityImageCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorage _storage;
    private readonly ILogger<DeleteCityImageCommandHandler> _logger;

    public DeleteCityImageCommandHandler(
        IApplicationDbContext db,
        IImageStorage storage,
        ILogger<DeleteCityImageCommandHandler> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteCityImageCommand command,
        CancellationToken cancellationToken)
    {
        var image = await _db.CityImages.FirstOrDefaultAsync(
            i => i.Id == command.ImageId && i.CityId == command.CityId,
            cancellationToken);

        if (image is null)
            return Result.Failure(CityImageErrors.NotFound(command.ImageId));

        var wasPrimary = image.IsPrimary;
        _db.CityImages.Remove(image);

        if (wasPrimary)
        {
            var next = await _db.CityImages
                .Where(i => i.CityId == command.CityId && i.Id != command.ImageId)
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
                "Cloudinary delete failed for CityImage {ImageId} publicId '{PublicId}'. Orphan will be cleaned later.",
                image.Id, image.PublicId);
        }

        return Result.Success();
    }
}