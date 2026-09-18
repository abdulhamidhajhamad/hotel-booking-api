using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Common.Messaging;

public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommandBase
    where TResponse : Result
{
    private readonly IApplicationDbContext _dbContext;

    public UnitOfWorkBehavior(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var response = await next();

            if (response.IsSuccess)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return response;
        });
    }
}