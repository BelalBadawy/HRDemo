using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Mediator;

namespace HrDemo.Application.Common.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "RefreshToken", "AccessToken", "Jwt", "Authorization", "Secret", "ClientSecret"
    };

    public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TMessage).Name;
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Starting request {RequestName} [CorrelationId: {CorrelationId}]. Payload: {@Payload}",
            requestName,
            correlationId,
            MaskSensitiveData(message)
        );

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next(message, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed request {RequestName} [CorrelationId: {CorrelationId}] in {ElapsedMilliseconds}ms.",
                requestName,
                correlationId,
                stopwatch.ElapsedMilliseconds
            );

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request {RequestName} [CorrelationId: {CorrelationId}] failed after {ElapsedMilliseconds}ms.",
                requestName,
                correlationId,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }

    private static Dictionary<string, object?> MaskSensitiveData(TMessage message)
    {
        var masked = new Dictionary<string, object?>();
        var properties = typeof(TMessage).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(message);
            if (SensitiveKeys.Contains(prop.Name))
            {
                masked[prop.Name] = "***[MASKED]***";
            }
            else
            {
                masked[prop.Name] = value;
            }
        }

        return masked;
    }
}
