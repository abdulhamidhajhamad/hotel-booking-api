using FluentValidation;
using FluentValidation.Results;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Common.Messaging;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
                failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
            return await next();

        var message = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
        var error = Error.Validation("Validation.Failed", message);
        return CreateFailure(error);
    }

    private static TResponse CreateFailure(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        var valueType = responseType.GetGenericArguments()[0];
        var closed = typeof(Result<>).MakeGenericType(valueType);
        var method = closed.GetMethod(
            nameof(Result<int>.Failure),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            new[] { typeof(Error) })!;
        return (TResponse)method.Invoke(null, new object[] { error })!;
    }
}