using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.HotelImages.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.HotelImages.SetPrimary;

public sealed class SetPrimaryHotelImageCommandHandler
    : ICommandHandler<SetPrimaryHotelImageCommand>
{
    private readonly IApplicationDbContext _db;

    public SetPrimaryHotelImageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(
        SetPrimaryHotelImageCommand command,
        CancellationToken cancellationToken)
    {
        var target = await _db.HotelImages.FirstOrDefaultAsync(
            i => i.Id == command.ImageId && i.HotelId == command.HotelId,
            cancellationToken);

        if (target is null)
            return Result.Failure(HotelImageErrors.NotFound(command.ImageId));

        if (target.IsPrimary)
            return Result.Success();

        var currentPrimaries = await _db.HotelImages
            .Where(i => i.HotelId == command.HotelId && i.IsPrimary)
            .ToListAsync(cancellationToken);

        foreach (var p in currentPrimaries) p.IsPrimary = false;
        target.IsPrimary = true;

        return Result.Success();
    }
}