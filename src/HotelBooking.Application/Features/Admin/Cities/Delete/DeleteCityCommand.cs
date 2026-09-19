using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Cities.Delete;

public sealed record DeleteCityCommand(Guid Id) : ICommand;