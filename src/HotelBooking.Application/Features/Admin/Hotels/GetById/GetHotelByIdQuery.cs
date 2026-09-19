using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Hotels.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.GetById;

public sealed record GetHotelByIdQuery(Guid Id) : IQuery<HotelDetail>;