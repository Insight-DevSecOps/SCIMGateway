// ==========================================================================
// T017: RateLimiter - Token Bucket Rate Limiting
// ==========================================================================
// Implements rate limiting with token bucket algorithm per tenant
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SCIMGateway.Core.Authentication;

/// <summary>
/// Interface for rate limiting.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Tries to acquire a token for the given key.
    /// </summary>
    /// <param name="key">Rate limit key (tenant ID, actor ID, etc.).</param>
    /// <returns>Rate limit result.</returns>
    Task<RateLimitResult> TryAcquireAsync(string key);

    /// <summary>
    /// Tries to acquire a token synchronously.
    /// </summary>
    /// <param name="key">Rate limit key.</param>
    /// <returns>Rate limit result.</returns>
    RateLimitResult TryAcquire(string key);

    /// <summary>
    /// Gets remaining tokens for a key.
    /// </summary>
    /// <param name="key">Rate limit key.</param>
    /// <returns>Number of remaining tokens.</returns>
    Task<int> GetRemainingTokensAsync(string key);

    /// <summary>
    /// Gets remaining tokens synchronously.
    /// </summary>
    /// <param name="key">Rate limit key.</param>
    /// <returns>Number of remaining tokens.</returns>
    int GetRemainingTokens(string key);

    /// <summary>
    /// Resets rate limit for a key.
    /// </summary>
    /// <param name="key">Rate limit key.</param>
    Task ResetAsync(string key);

    /// <summary>
    /// Resets rate limit synchronously.
    /// </summary>
    /// <param name="key">Rate limit key.</param>
    void Reset(string key);

    /// <summary>
    /// Checks if the request should be allowed based on rate limits.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="actorId">Actor identifier (optional for per-actor limits).</param>
    /// <returns>Rate limit result.</returns>
    RateLimitResult CheckRateLimit(string tenantId, string? actorId = null);

    /// <summary>
    /// Records a failed authentication attempt.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="actorId">Actor identifier.</param>
    /// <param name="ipAddress">IP address of the request.</param>
    Task RecordAuthFailureAsync(string tenantId, string? actorId, string? ipAddress);

    /// <summary>
    /// Records a failed authentication attempt synchronously.
    /// </summary>
    void RecordAuthFailure(string tenantId, string? actorId, string? ipAddress);

    /// <summary>
    /// Checks if the actor is locked out due to failed auth attempts.
    /// </summary>
    /// <param name="key">Rate limit key.</param>
    /// <returns>True if locked out.</returns>
    Task<bool> IsLockedOutAsync(string key);

    /// <summary>
    /// Checks if the actor is locked out synchronously.
    /// </summary>
    bool IsLockedOut(string key);

    /// <summary>
    /// Checks lockout status.
    /// </summary>
    LockoutStatus CheckLockout(string tenantId, string? actorId, string? ipAddress);

    /// <summary>
    /// Clears lockout for a key.
    /// </summary>
    /// <param name="key">Rate limit key.</param>
    Task ClearLockoutAsync(string key);

    /// <summary>
    /// Clears lockout synchronously.
    /// </summary>
    void ClearLockout(string tenantId, string? actorId);
}

/// <summary>
/// Result of a rate limit check.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Number of remaining tokens in the bucket.
    /// </summary>
    public int RemainingTokens { get; set; }

    /// <summary>
    /// Number of remaining requests (alias for RemainingTokens).
    /// </summary>
    public int RemainingRequests { get => RemainingTokens; set => RemainingTokens = value; }

    /// <summary>
    /// Total request limit per window.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// When the rate limit window resets.
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// Alias for ResetTime.
    /// </summary>
    public DateTime ResetAt { get => ResetTime; set => ResetTime = value; }

    /// <summary>
    /// Seconds until the rate limit resets.
    /// </summary>
    public int RetryAfter { get; set; }

    /// <summary>
    /// Alias for RetryAfter.
    /// </summary>
    public int RetryAfterSeconds { get => RetryAfter; set => RetryAfter = value; }

    /// <summary>
    /// Reason for rate limiting (if not allowed).
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Lockout status for an actor.
/// </summary>
public class LockoutStatus
{
    /// <summary>
    /// Whether the actor is locked out.
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Number of failed attempts.
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Maximum allowed failures before lockout.
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// When the lockout expires.
    /// </summary>
    public DateTime? LockoutEndsAt { get; set; }

    /// <summary>
    /// Seconds until lockout ends.
    /// </summary>
    public int? RetryAfterSeconds { get; set; }
}

/// <summary>
/// Options for rate limiting.
/// </summary>
public class RateLimiterOptions
{
    /// <summary>
    /// Maximum requests per minute per tenant.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 1000;

    /// <summary>
    /// Maximum requests per second per tenant.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; } = 100;

    /// <summary>
    /// Token bucket capacity.
    /// </summary>
    public int BucketCapacity { get; set; } = 100;

    /// <summary>
    /// Tokens added per second (refill rate).
    /// </summary>
    public double TokensPerSecond { get; set; } = 20;

    /// <summary>
    /// Token refill rate per second (alias for TokensPerSecond).
    /// </summary>
    public double RefillRatePerSecond { get => TokensPerSecond; set => TokensPerSecond = value; }

    /// <summary>
    /// Time window for rate limiting.
    /// </summary>
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum failed auth attempts before lockout.
    /// </summary>
    public int MaxAuthFailures { get; set; } = 5;

    /// <summary>
    /// Lockout duration after max failures.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Window for counting auth failures.
    /// </summary>
    public TimeSpan AuthFailureWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to apply per-actor rate limits.
    /// </summary>
    public bool EnablePerActorLimits { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute per actor.
    /// </summary>
    public int MaxRequestsPerActorPerMinute { get; set; } = 200;

    /// <summary>
    /// Per-tenant limit configurations.
    /// </summary>
    public Dictionary<string, TenantRateLimitConfig>? TenantLimits { get; set; }
}

/// <summary>
/// Per-tenant rate limit configuration.
/// </summary>
public class TenantRateLimitConfig
{
    /// <summary>
    /// Maximum requests per minute for this tenant.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; }

    /// <summary>
    /// Bucket capacity for this tenant.
    /// </summary>
    public int BucketCapacity { get; set; }
}

/// <summary>
/// Constants for rate limit HTTP headers.
/// </summary>
public static class RateLimitHeaders
{
    /// <summary>X-RateLimit-Limit header name.</summary>
    public const string RateLimitLimit = "X-RateLimit-Limit";

    /// <summary>Alias for RateLimitLimit.</summary>
    public const string Limit = "X-RateLimit-Limit";

    /// <summary>X-RateLimit-Remaining header name.</summary>
    public const string RateLimitRemaining = "X-RateLimit-Remaining";

    /// <summary>Alias for RateLimitRemaining.</summary>
    public const string Remaining = "X-RateLimit-Remaining";

    /// <summary>X-RateLimit-Reset header name.</summary>
    public const string RateLimitReset = "X-RateLimit-Reset";

    /// <summary>Alias for RateLimitReset.</summary>
    public const string Reset = "X-RateLimit-Reset";

    /// <summary>Retry-After header name.</summary>
    public const string RetryAfter = "Retry-After";
}

/// <summary>
/// Rate limiter implementation using token bucket algorithm.
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly RateLimiterOptions _options;
    private readonly ILogger<RateLimiter> _logger;
    private readonly ConcurrentDictionary<string, TokenBucket> _tenantBuckets = new();
    private readonly ConcurrentDictionary<string, TokenBucket> _actorBuckets = new();
    private readonly ConcurrentDictionary<string, AuthFailureTracker> _authFailures = new();
    private readonly IRateLimitStore? _distributedStore;

    public RateLimiter(IOptions<RateLimiterOptions> options, ILogger<RateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Constructor with distributed store for multi-instance deployments.
    /// </summary>
    public RateLimiter(IOptions<RateLimiterOptions> options, ILogger<RateLimiter> logger, IRateLimitStore distributedStore)
        : this(options, logger)
    {
        _distributedStore = distributedStore;
    }

    /// <inheritdoc />
    public RateLimitResult CheckRateLimit(string tenantId, string? actorId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        // Check tenant rate limit
        var tenantBucket = _tenantBuckets.GetOrAdd(tenantId, _ => new TokenBucket(
            _options.BucketCapacity,
            _options.RefillRatePerSecond));

        if (!tenantBucket.TryConsume())
        {
            var resetTime = tenantBucket.GetNextRefillTime();
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId}", tenantId);
            
            return new RateLimitResult
            {
                IsAllowed = false,
                RemainingRequests = 0,
                Limit = _options.BucketCapacity,
                ResetAt = resetTime,
                RetryAfterSeconds = (int)Math.Ceiling((resetTime - DateTime.UtcNow).TotalSeconds),
                Reason = "Tenant rate limit exceeded"
            };
        }

        // Check per-actor rate limit if enabled
        if (_options.EnablePerActorLimits && !string.IsNullOrEmpty(actorId))
        {
            var actorKey = $"{tenantId}:{actorId}";
            var actorBucket = _actorBuckets.GetOrAdd(actorKey, _ => new TokenBucket(
                _options.MaxRequestsPerActorPerMinute / 60,
                _options.MaxRequestsPerActorPerMinute / 60.0));

            if (!actorBucket.TryConsume())
            {
                var resetTime = actorBucket.GetNextRefillTime();
                _logger.LogWarning("Rate limit exceeded for actor {ActorId} in tenant {TenantId}", actorId, tenantId);
                
                return new RateLimitResult
                {
                    IsAllowed = false,
                    RemainingRequests = 0,
                    Limit = _options.MaxRequestsPerActorPerMinute / 60,
                    ResetAt = resetTime,
                    RetryAfterSeconds = (int)Math.Ceiling((resetTime - DateTime.UtcNow).TotalSeconds),
                    Reason = "Actor rate limit exceeded"
                };
            }
        }

        return new RateLimitResult
        {
            IsAllowed = true,
            RemainingRequests = tenantBucket.GetRemainingTokens(),
            Limit = _options.BucketCapacity,
            ResetAt = tenantBucket.GetNextRefillTime()
        };
    }

    /// <inheritdoc />
    public void RecordAuthFailure(string tenantId, string? actorId, string? ipAddress)
    {
        var key = GetAuthFailureKey(tenantId, actorId, ipAddress);
        var tracker = _authFailures.GetOrAdd(key, _ => new AuthFailureTracker(_options.AuthFailureWindow));
        tracker.RecordFailure();

        _logger.LogWarning("Auth failure recorded for {Key}, total failures: {Count}", key, tracker.FailureCount);

        if (tracker.FailureCount >= _options.MaxAuthFailures)
        {
            tracker.LockUntil = DateTime.UtcNow.Add(_options.LockoutDuration);
            _logger.LogWarning("Account locked for {Key} until {LockUntil}", key, tracker.LockUntil);
        }
    }

    /// <inheritdoc />
    public LockoutStatus CheckLockout(string tenantId, string? actorId, string? ipAddress)
    {
        var key = GetAuthFailureKey(tenantId, actorId, ipAddress);
        
        if (!_authFailures.TryGetValue(key, out var tracker))
        {
            return new LockoutStatus
            {
                IsLockedOut = false,
                FailedAttempts = 0,
                MaxAttempts = _options.MaxAuthFailures
            };
        }

        // Clean up old failures
        tracker.CleanupOldFailures();

        var isLockedOut = tracker.LockUntil.HasValue && tracker.LockUntil > DateTime.UtcNow;

        return new LockoutStatus
        {
            IsLockedOut = isLockedOut,
            FailedAttempts = tracker.FailureCount,
            MaxAttempts = _options.MaxAuthFailures,
            LockoutEndsAt = tracker.LockUntil,
            RetryAfterSeconds = isLockedOut 
                ? (int)Math.Ceiling((tracker.LockUntil!.Value - DateTime.UtcNow).TotalSeconds)
                : null
        };
    }

    /// <inheritdoc />
    public void ClearLockout(string tenantId, string? actorId)
    {
        var key = GetAuthFailureKey(tenantId, actorId, null);
        _authFailures.TryRemove(key, out _);
        _logger.LogInformation("Lockout cleared for {Key}", key);
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> TryAcquireAsync(string key)
    {
        return await Task.FromResult(TryAcquire(key));
    }

    /// <inheritdoc />
    public RateLimitResult TryAcquire(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return CheckRateLimit(key);
    }

    /// <inheritdoc />
    public async Task<int> GetRemainingTokensAsync(string key)
    {
        return await Task.FromResult(GetRemainingTokens(key));
    }

    /// <inheritdoc />
    public int GetRemainingTokens(string key)
    {
        if (_tenantBuckets.TryGetValue(key, out var bucket))
        {
            return bucket.GetRemainingTokens();
        }
        return _options.BucketCapacity;
    }

    /// <inheritdoc />
    public async Task ResetAsync(string key)
    {
        await Task.Run(() => Reset(key));
    }

    /// <inheritdoc />
    public void Reset(string key)
    {
        _tenantBuckets.TryRemove(key, out _);
        _actorBuckets.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public async Task RecordAuthFailureAsync(string tenantId, string? actorId, string? ipAddress)
    {
        await Task.Run(() => RecordAuthFailure(tenantId, actorId, ipAddress));
    }

    /// <inheritdoc />
    public async Task<bool> IsLockedOutAsync(string key)
    {
        return await Task.FromResult(IsLockedOut(key));
    }

    /// <inheritdoc />
    public bool IsLockedOut(string key)
    {
        if (_authFailures.TryGetValue(key, out var tracker))
        {
            tracker.CleanupOldFailures();
            return tracker.LockUntil.HasValue && tracker.LockUntil > DateTime.UtcNow;
        }
        return false;
    }

    /// <inheritdoc />
    public async Task ClearLockoutAsync(string key)
    {
        await Task.Run(() => 
        {
            _authFailures.TryRemove(key, out _);
            _logger.LogInformation("Lockout cleared for {Key}", key);
        });
    }

    private static string GetAuthFailureKey(string tenantId, string? actorId, string? ipAddress)
    {
        if (!string.IsNullOrEmpty(actorId))
            return $"actor:{tenantId}:{actorId}";
        if (!string.IsNullOrEmpty(ipAddress))
            return $"ip:{tenantId}:{ipAddress}";
        return $"tenant:{tenantId}";
    }

    /// <summary>
    /// Token bucket implementation for rate limiting.
    /// </summary>
    private class TokenBucket
    {
        private readonly int _capacity;
        private readonly double _refillRatePerSecond;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly object _lock = new();

        public TokenBucket(int capacity, double refillRatePerSecond)
        {
            _capacity = capacity;
            _refillRatePerSecond = refillRatePerSecond;
            _tokens = capacity;
            _lastRefill = DateTime.UtcNow;
        }

        public bool TryConsume(int count = 1)
        {
            lock (_lock)
            {
                Refill();

                if (_tokens >= count)
                {
                    _tokens -= count;
                    return true;
                }

                return false;
            }
        }

        public int GetRemainingTokens()
        {
            lock (_lock)
            {
                Refill();
                return (int)_tokens;
            }
        }

        public DateTime GetNextRefillTime()
        {
            lock (_lock)
            {
                if (_tokens > 0)
                    return DateTime.UtcNow;

                var tokensNeeded = 1 - _tokens;
                var secondsUntilRefill = tokensNeeded / _refillRatePerSecond;
                return DateTime.UtcNow.AddSeconds(secondsUntilRefill);
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefill).TotalSeconds;
            var tokensToAdd = elapsed * _refillRatePerSecond;
            
            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }

    /// <summary>
    /// Tracks authentication failures for lockout logic.
    /// </summary>
    private class AuthFailureTracker
    {
        private readonly TimeSpan _window;
        private readonly List<DateTime> _failures = [];
        private readonly object _lock = new();

        public DateTime? LockUntil { get; set; }

        public int FailureCount
        {
            get
            {
                lock (_lock)
                {
                    CleanupOldFailures();
                    return _failures.Count;
                }
            }
        }

        public AuthFailureTracker(TimeSpan window)
        {
            _window = window;
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                CleanupOldFailures();
                _failures.Add(DateTime.UtcNow);
            }
        }

        public void CleanupOldFailures()
        {
            var cutoff = DateTime.UtcNow - _window;
            _failures.RemoveAll(f => f < cutoff);

            // Clear lockout if it has expired
            if (LockUntil.HasValue && LockUntil < DateTime.UtcNow)
            {
                LockUntil = null;
                _failures.Clear();
            }
        }
    }
}

/// <summary>
/// Interface for distributed rate limit storage.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Gets remaining tokens for a key.
    /// </summary>
    Task<int> GetRemainingTokensAsync(string key);

    /// <summary>
    /// Tries to consume tokens.
    /// </summary>
    Task<bool> TryConsumeAsync(string key, int tokens = 1);

    /// <summary>
    /// Resets tokens for a key.
    /// </summary>
    Task ResetAsync(string key);
}
