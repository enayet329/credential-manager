namespace CredVault.Application.Abstractions.RateLimiting;

/// <summary>In-process rate limiter keyed by a string partition. Returns true when the call is allowed.</summary>
public interface IInProcessRateLimiter
{
    /// <summary>Attempts to acquire a permit for the given partition.</summary>
    /// <param name="partitionKey">Logical partition (e.g. <c>"cred:{id}"</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RateLimitLease> AcquireAsync(string partitionKey, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of <see cref="IInProcessRateLimiter.AcquireAsync"/>.</summary>
/// <param name="IsAcquired">Whether the call is permitted.</param>
/// <param name="RetryAfter">Suggested wait before retrying when not acquired.</param>
public readonly record struct RateLimitLease(bool IsAcquired, TimeSpan RetryAfter);
