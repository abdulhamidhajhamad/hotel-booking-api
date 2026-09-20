using MediatR;
using Microsoft.Extensions.Logging;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IApplicationDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand(typeof(TRequest)))
            return await next();

        var requestName = typeof(TRequest).Name;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (response.IsSuccess)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Committed transaction for {RequestName}", requestName);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogInformation("Rolled back transaction for {RequestName}: {ErrorCode}",
                    requestName, response.Error.Code);
            }

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsCommand(Type requestType)
    {
        if (typeof(ICommand).IsAssignableFrom(requestType))
            return true;

        return requestType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }
}