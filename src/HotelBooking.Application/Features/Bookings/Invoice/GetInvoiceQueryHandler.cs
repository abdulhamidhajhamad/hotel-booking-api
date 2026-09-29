using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Application.Features.Bookings.Invoice.Abstractions;

namespace HotelBooking.Application.Features.Bookings.Invoice;

public sealed class GetInvoiceQueryHandler : IQueryHandler<GetInvoiceQuery, InvoiceModel>
{
    private readonly IInvoiceReader _reader;
    private readonly ICurrentUser _currentUser;
    private readonly InvoiceBuilder _invoiceBuilder;

    public GetInvoiceQueryHandler(
        IInvoiceReader reader,
        ICurrentUser currentUser,
        InvoiceBuilder invoiceBuilder)
    {
        _reader = reader;
        _currentUser = currentUser;
        _invoiceBuilder = invoiceBuilder;
    }

    public async Task<Result<InvoiceModel>> Handle(
        GetInvoiceQuery query,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Result<InvoiceModel>.Failure(BookingErrors.NotAuthenticated());

        var ownerId = await _reader.GetOwnerIdAsync(query.BookingGroupId, cancellationToken);

        if (ownerId is null)
            return Result<InvoiceModel>.Failure(BookingErrors.BookingGroupNotFound(query.BookingGroupId));

        if (ownerId != userId)
            return Result<InvoiceModel>.Failure(BookingErrors.InvoiceForbidden());

        var invoice = await _invoiceBuilder.BuildAsync(query.BookingGroupId, cancellationToken);

        return invoice is null
            ? Result<InvoiceModel>.Failure(BookingErrors.BookingGroupNotFound(query.BookingGroupId))
            : Result<InvoiceModel>.Success(invoice);
    }
}
