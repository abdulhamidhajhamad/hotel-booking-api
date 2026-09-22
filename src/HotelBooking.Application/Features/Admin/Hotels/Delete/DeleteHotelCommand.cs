using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Hotels.Delete;

public sealed record DeleteHotelCommand(Guid Id) : ICommand;