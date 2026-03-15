using Polly;

namespace CartBuddy.Services;

public static class PollyHelper
{
    private const int SearchRetryCount = 3;
    private const int InitialRetryDelayMs = 500;
    private const int MaxRetryDelayMs = 4000;

    private static readonly AsyncPolicy SearchRetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            SearchRetryCount,
            retryAttempt =>
            {
                var delayMs = Math.Min(
                    MaxRetryDelayMs,
                    InitialRetryDelayMs * (1 << (retryAttempt - 1))
                );
                return TimeSpan.FromMilliseconds(delayMs);
            }
        );

    public static Task<T> ExecuteSearchRetry<T>(Func<Task<T>> action)
    {
        return SearchRetryPolicy.ExecuteAsync(action);
    }
}