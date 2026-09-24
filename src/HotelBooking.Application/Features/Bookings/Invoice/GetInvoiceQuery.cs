using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Bookings.Invoice;

public sealed record GetInvoiceQuery(Guid BookingGroupId) : IQuery<InvoiceModel>;