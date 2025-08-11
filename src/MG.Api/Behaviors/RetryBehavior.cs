using MediatR;
using Polly;
using FluentValidation;

namespace MG.Api.Behaviors;

public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        
        // Configure retry policy with reduced timing for faster retries
        // Exclude client errors that should not be retried
        _retryPolicy = Policy.Handle<Exception>(ex => 
                !IsNonRetryableException(ex))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(500 * retryAttempt), // 500ms, 1s, 1.5s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for operation {Operation} in {Delay}ms. Exception: {ExceptionType}",
                        retryCount,
                        context.OperationKey ?? typeof(TRequest).Name,
                        timespan.TotalMilliseconds,
                        outcome.GetType().Name);
                });
    }

    private static bool IsNonRetryableException(Exception exception)
    {
        return exception is UnauthorizedAccessException 
            or ValidationException
            or ArgumentException
            or ArgumentNullException
            or InvalidOperationException;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteAsync(async () => await next());
    }
}
