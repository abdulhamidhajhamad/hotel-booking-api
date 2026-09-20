using MediatR;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Common.Messaging;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}