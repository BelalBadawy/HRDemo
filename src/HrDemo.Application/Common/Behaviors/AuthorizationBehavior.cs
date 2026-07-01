using System.Reflection;
using Mediator;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ICurrentUser _currentUser;

    public AuthorizationBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (message is IAuthorizeRequest authorizeRequest)
        {
            if (!_currentUser.IsAuthenticated)
            {
                return CreateFailureResponse(ResultStatus.Unauthorized, "User is not authenticated.", 401);
            }

            foreach (var permission in authorizeRequest.RequiredPermissions)
            {
                if (!_currentUser.HasPermission(permission))
                {
                    return CreateFailureResponse(ResultStatus.Forbidden, $"User does not have the required permission: {permission}", 403);
                }
            }
        }

        return await next(message, cancellationToken);
    }

    private static TResponse CreateFailureResponse(ResultStatus status, string message, int statusCode)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(ResponseResult))
        {
            return (TResponse)(object)ResponseResult.FailureResult(status, message, statusCode);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ResponseResult<>))
        {
            var genericArgs = responseType.GetGenericArguments();
            var method = typeof(ResponseResult<>)
                .MakeGenericType(genericArgs)
                .GetMethod("FailureResult", BindingFlags.Public | BindingFlags.Static);

            if (method != null)
            {
                var result = method.Invoke(null, new object?[] { status, message, statusCode, null });
                return (TResponse)result!;
            }
        }

        throw new UnauthorizedAccessException(message);
    }
}
