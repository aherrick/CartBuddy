using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CartBuddy.Server;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddCartBuddyRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(
                            retryAfter.TotalSeconds
                        )
                        .ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    return RateLimitPartition.GetNoLimiter("non-api");
                }

                return GetIpLimiter(context, "api", 100, TimeSpan.FromMinutes(1));
            });

            options.AddPolicy(
                "checkout",
                context =>
                {
                    return GetIpLimiter(context, "checkout", 10, TimeSpan.FromMinutes(5));
                }
            );
        });

        return services;
    }

    private static RateLimitPartition<string> GetIpLimiter(
        HttpContext context,
        string prefix,
        int permitLimit,
        TimeSpan window
    )
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"{prefix}:{ipAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }
        );
    }
}