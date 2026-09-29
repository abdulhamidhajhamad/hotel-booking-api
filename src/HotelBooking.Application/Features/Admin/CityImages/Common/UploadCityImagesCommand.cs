using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Features.Admin.CityImages.Common;

namespace HotelBooking.Application.Features.Admin.CityImages.Upload;

public sealed record UploadCityImagesCommand(
    Guid CityId,
    IReadOnlyList<UploadImageFile> Files) : ICommand<IReadOnlyList<CityImageDto>>, IManagesOwnTransactions;