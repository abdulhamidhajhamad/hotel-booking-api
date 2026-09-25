using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.CityImages.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.CityImages.Upload;

public sealed class UploadCityImagesCommandHandler
    : ICommandHandler<UploadCityImagesCommand, IReadOnlyList<CityImageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IImageStorage _storage;

    public UploadCityImagesCommandHandler(IApplicationDbContext db, IImageStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Result<IReadOnlyList<CityImageDto>>> Handle(
        UploadCityImagesCommand command,
        CancellationToken cancellationToken)
    {
        var cityExists = await _db.Cities
            .AnyAsync(c => c.Id == command.CityId, cancellationToken);
        if (!cityExists)
            return CityImageErrors.CityNotFound(command.CityId);

        var hasPrimary = await _db.CityImages
            .AnyAsync(i => i.CityId == command.CityId && i.IsPrimary, cancellationToken);

        var items = new List<CityImageDto>();
        var isFirstInBatch = true;

        foreach (var file in command.Files)
        {
            var stored = await _storage.UploadAsync(
                file.Content,
                file.ContentType,
                $"cities/{command.CityId}",
                cancellationToken);

            var makePrimary = !hasPrimary && isFirstInBatch;

            var image = new CityImage
            {
                CityId = command.CityId,
                Url = stored.Url,
                PublicId = stored.PublicId,
                IsPrimary = makePrimary,
            };
            await _db.CityImages.AddAsync(image, cancellationToken);

            if (makePrimary) hasPrimary = true;
            isFirstInBatch = false;

            items.Add(new CityImageDto(image.Id, image.Url, image.IsPrimary));
        }

        return Result<IReadOnlyList<CityImageDto>>.Success(items);
    }
}