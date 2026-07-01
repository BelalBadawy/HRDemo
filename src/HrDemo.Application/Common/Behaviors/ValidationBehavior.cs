using System.Reflection;
using FluentValidation;
using Mediator;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Common.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TMessage>> validators)
    {
        _validators = validators;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

            return CreateFailureResponse(errors);
        }

        return await next(message, cancellationToken);
    }

    private static TResponse CreateFailureResponse(Dictionary<string, string[]> errors)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(ResponseResult))
        {
            return (TResponse)(object)ResponseResult.FailureResult(
                ResultStatus.ValidationError,
                "Validation failures occurred.",
                400,
                errors
            );
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ResponseResult<>))
        {
            var genericArgs = responseType.GetGenericArguments();
            var method = typeof(ResponseResult<>)
                .MakeGenericType(genericArgs)
                .GetMethod("FailureResult", BindingFlags.Public | BindingFlags.Static);

            if (method != null)
            {
                var result = method.Invoke(null, new object?[] { ResultStatus.ValidationError, "Validation failures occurred.", 400, errors });
                return (TResponse)result!;
            }
        }

        throw new ValidationException("Validation failures occurred.");
    }
}
