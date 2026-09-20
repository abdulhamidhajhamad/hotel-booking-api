using MediatR;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}