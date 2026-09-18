using System.Collections.Concurrent;
using HotelBooking.Application.Common.Results;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application.Common.Messaging;

public sealed class Dispatcher : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> WrapperCache = new();
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task<Result> Send(ICommand command, CancellationToken cancellationToken)
    {
        var wrapper = (VoidCommandWrapper)WrapperCache.GetOrAdd(
            command.GetType(),
            t => Activator.CreateInstance(typeof(VoidCommandWrapperImpl<>).MakeGenericType(t))!);
        return wrapper.Handle(command, _serviceProvider, cancellationToken);
    }

    public Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken)
    {
        var wrapper = (CommandWrapper<TResponse>)WrapperCache.GetOrAdd(
            command.GetType(),
            t => Activator.CreateInstance(
                typeof(CommandWrapperImpl<,>).MakeGenericType(t, typeof(TResponse)))!);
        return wrapper.Handle(command, _serviceProvider, cancellationToken);
    }

    public Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken)
    {
        var wrapper = (QueryWrapper<TResponse>)WrapperCache.GetOrAdd(
            query.GetType(),
            t => Activator.CreateInstance(
                typeof(QueryWrapperImpl<,>).MakeGenericType(t, typeof(TResponse)))!);
        return wrapper.Handle(query, _serviceProvider, cancellationToken);
    }

    private abstract class VoidCommandWrapper
    {
        public abstract Task<Result> Handle(ICommand command, IServiceProvider sp, CancellationToken ct);
    }

    private sealed class VoidCommandWrapperImpl<TCommand> : VoidCommandWrapper
        where TCommand : ICommand
    {
        public override async Task<Result> Handle(ICommand command, IServiceProvider sp, CancellationToken ct)
        {
            var typed = (TCommand)command;
            var handler = sp.GetRequiredService<ICommandHandler<TCommand>>();
            var behaviors = sp.GetServices<IPipelineBehavior<TCommand, Result>>().Reverse().ToArray();

            RequestHandlerDelegate<Result> pipeline = () => handler.Handle(typed, ct);
            foreach (var behavior in behaviors)
            {
                var next = pipeline;
                var current = behavior;
                pipeline = () => current.Handle(typed, next, ct);
            }
            return await pipeline();
        }
    }

    private abstract class CommandWrapper<TResponse>
    {
        public abstract Task<Result<TResponse>> Handle(object command, IServiceProvider sp, CancellationToken ct);
    }

    private sealed class CommandWrapperImpl<TCommand, TResponse> : CommandWrapper<TResponse>
        where TCommand : ICommand<TResponse>
    {
        public override async Task<Result<TResponse>> Handle(object command, IServiceProvider sp, CancellationToken ct)
        {
            var typed = (TCommand)command;
            var handler = sp.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            var behaviors = sp.GetServices<IPipelineBehavior<TCommand, Result<TResponse>>>().Reverse().ToArray();

            RequestHandlerDelegate<Result<TResponse>> pipeline = () => handler.Handle(typed, ct);
            foreach (var behavior in behaviors)
            {
                var next = pipeline;
                var current = behavior;
                pipeline = () => current.Handle(typed, next, ct);
            }
            return await pipeline();
        }
    }

    private abstract class QueryWrapper<TResponse>
    {
        public abstract Task<Result<TResponse>> Handle(object query, IServiceProvider sp, CancellationToken ct);
    }

    private sealed class QueryWrapperImpl<TQuery, TResponse> : QueryWrapper<TResponse>
        where TQuery : IQuery<TResponse>
    {
        public override async Task<Result<TResponse>> Handle(object query, IServiceProvider sp, CancellationToken ct)
        {
            var typed = (TQuery)query;
            var handler = sp.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
            var behaviors = sp.GetServices<IPipelineBehavior<TQuery, Result<TResponse>>>().Reverse().ToArray();

            RequestHandlerDelegate<Result<TResponse>> pipeline = () => handler.Handle(typed, ct);
            foreach (var behavior in behaviors)
            {
                var next = pipeline;
                var current = behavior;
                pipeline = () => current.Handle(typed, next, ct);
            }
            return await pipeline();
        }
    }
}