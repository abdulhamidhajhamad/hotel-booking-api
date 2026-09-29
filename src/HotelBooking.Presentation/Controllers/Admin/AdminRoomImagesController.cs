using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Features.Admin.RoomImages.Common;
using HotelBooking.Application.Features.Admin.RoomImages.Delete;
using HotelBooking.Application.Features.Admin.RoomImages.Upload;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/hotels/{hotelId:guid}/rooms/{roomId:guid}/images")]
[Tags("Admin - Rooms")]
public sealed class AdminRoomImagesController : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly IDispatcher _dispatcher;

    public AdminRoomImagesController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(120 * 1024 * 1024)]
    [ProducesResponseType(typeof(IReadOnlyList<RoomImageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        Guid hotelId,
        Guid roomId,
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "No files uploaded",
                Detail = "At least one file must be provided.",
            });

        var uploads = new List<UploadImageFile>();
        foreach (var f in files)
        {
            if (f.Length > MaxFileSizeBytes)
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "File too large",
                    Detail = $"File '{f.FileName}' exceeds the 5 MB limit.",
                });

            uploads.Add(new UploadImageFile(f.OpenReadStream(), f.ContentType, f.FileName));
        }

        var result = await _dispatcher.Send(
            new UploadRoomImagesCommand(hotelId, roomId, uploads),
            cancellationToken);

        return result.IsSuccess
            ? Created($"/api/v1/admin/hotels/{hotelId}/rooms/{roomId}/images", result.Value)
            : result.ToActionResult();
    }

    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid hotelId,
        Guid roomId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(
            new DeleteRoomImageCommand(hotelId, roomId, imageId),
            cancellationToken);

        return result.ToActionResult();
    }
}