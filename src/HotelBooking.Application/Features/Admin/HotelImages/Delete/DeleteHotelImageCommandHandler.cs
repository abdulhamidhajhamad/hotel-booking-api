using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.HotelImages.Abstractions;
using HotelBooking.Application.Features.Admin.HotelImages.Common;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Features.Admin.HotelImages.Delete;

public sealed class DeleteHotelImageCommandHandler
    : ICommandHandler<DeleteHotelImageCommand>
{
    private readonly IHotelImageRepository _images;
    private readonly IImageStorage _storage;
    private readonly ILogger<DeleteHotelImageCommandHandler> _logger;

    public DeleteHotelImageCommandHandler(
        IHotelImageRepository images,
        IImageStorage storage,
        ILogger<DeleteHotelImageCommandHandler> logger)
    {
        _images = images;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteHotelImageCommand command,
        CancellationToken cancellationToken)
    {
        var image = await _images.GetByIdAsync(command.ImageId, command.HotelId, cancellationToken);

        if (image is null)
            return Result.Failure(HotelImageErrors.NotFound(command.ImageId));

        var wasPrimary = image.IsPrimary;
        _images.Remove(image);

        if (wasPrimary)
        {
            var next = await _images.GetNextForPrimaryAsync(command.HotelId, command.ImageId, cancellationToken);
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
