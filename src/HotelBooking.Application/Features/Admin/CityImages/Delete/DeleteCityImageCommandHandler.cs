using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.CityImages.Abstractions;
using HotelBooking.Application.Features.Admin.CityImages.Common;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.CityImages.Delete;

public sealed class DeleteCityImageCommandHandler
    : ICommandHandler<DeleteCityImageCommand>
{
    private readonly ICityImageRepository _images;
    private readonly IImageStorage _storage;
    private readonly ILogger<DeleteCityImageCommandHandler> _logger;

    public DeleteCityImageCommandHandler(
        ICityImageRepository images,
        IImageStorage storage,
        ILogger<DeleteCityImageCommandHandler> logger)
    {
        _images = images;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteCityImageCommand command,
        CancellationToken cancellationToken)
    {
        var image = await _images.GetByIdAsync(command.ImageId, command.CityId, cancellationToken);

        if (image is null)
            return Result.Failure(CityImageErrors.NotFound(command.ImageId));

        var wasPrimary = image.IsPrimary;
        _images.Remove(image);

        if (wasPrimary)
        {
            var next = await _images.GetNextForPrimaryAsync(command.CityId, command.ImageId, cancellationToken);
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
