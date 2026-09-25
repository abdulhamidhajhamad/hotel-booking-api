using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Features.Admin.CityImages.Common;
using HotelBooking.Application.Features.Admin.CityImages.Delete;
using HotelBooking.Application.Features.Admin.CityImages.Upload;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/cities/{cityId:guid}/images")]
public sealed class AdminCityImagesController : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly IDispatcher _dispatcher;

    public AdminCityImagesController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(120 * 1024 * 1024)]
    [ProducesResponseType(typeof(IReadOnlyList<CityImageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        Guid cityId,
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
            new UploadCityImagesCommand(cityId, uploads),
            cancellationToken);

        return result.IsSuccess
            ? Created($"/api/v1/admin/cities/{cityId}/images", result.Value)
            : result.ToActionResult();
    }

    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid cityId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(
            new DeleteCityImageCommand(cityId, imageId),
            cancellationToken);

        return result.ToActionResult();
    }
}