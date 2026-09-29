using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Features.Admin.HotelImages.Common;

namespace HotelBooking.Application.Features.Admin.HotelImages.Upload;

public sealed record UploadHotelImagesCommand(
    Guid HotelId,
    IReadOnlyList<UploadImageFile> Files) : ICommand<IReadOnlyList<HotelImageDto>>, IManagesOwnTransactions;