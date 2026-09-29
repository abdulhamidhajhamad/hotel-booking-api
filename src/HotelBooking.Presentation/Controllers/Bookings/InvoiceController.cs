using HotelBooking.Application.Abstractions.Invoicing;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Bookings.Invoice;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Bookings;

[ApiController]
[Authorize]
[Route("api/v1/bookings/{bookingGroupId:guid}/invoice")]
[Tags("Bookings")]
public sealed class InvoiceController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IInvoiceRenderer _invoiceRenderer;

    public InvoiceController(IDispatcher dispatcher, IInvoiceRenderer invoiceRenderer)
    {
        _dispatcher = dispatcher;
        _invoiceRenderer = invoiceRenderer;
    }

    [HttpGet]
    [ProducesResponseType(typeof(InvoiceModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid bookingGroupId, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetInvoiceQuery(bookingGroupId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(Guid bookingGroupId, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetInvoiceQuery(bookingGroupId), cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult();

        var pdf = _invoiceRenderer.RenderPdf(result.Value);
        return File(pdf, "application/pdf", $"invoice-{result.Value.ConfirmationNumber}.pdf");
    }
}