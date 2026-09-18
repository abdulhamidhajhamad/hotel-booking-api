using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Common.Messaging;

public interface IDispatcher
{
    Task<Result> Send(ICommand command, CancellationToken cancellationToken);
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken);
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken);
}