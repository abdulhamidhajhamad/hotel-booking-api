using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.HotelImages.Abstractions;
using HotelBooking.Application.Features.Admin.HotelImages.Common;

namespace HotelBooking.Application.Features.Admin.HotelImages.SetPrimary;

public sealed class SetPrimaryHotelImageCommandHandler
    : ICommandHandler<SetPrimaryHotelImageCommand>
{
    private readonly IHotelImageRepository _images;

    public SetPrimaryHotelImageCommandHandler(IHotelImageRepository images) => _images = images;

    public async Task<Result> Handle(
        SetPrimaryHotelImageCommand command,
        CancellationToken cancellationToken)
    {
        var target = await _images.GetByIdAsync(command.ImageId, command.HotelId, cancellationToken);

        if (target is null)
            return Result.Failure(HotelImageErrors.NotFound(command.ImageId));

        if (target.IsPrimary)
            return Result.Success();

        var currentPrimaries = await _images.GetPrimariesAsync(command.HotelId, cancellationToken);

        foreach (var p in currentPrimaries) p.IsPrimary = false;
        target.IsPrimary = true;

        return Result.Success();
    }
}
