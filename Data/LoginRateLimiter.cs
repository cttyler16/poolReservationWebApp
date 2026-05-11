using System.Collections.Concurrent;

namespace PoolReservationWeb.Data;

/// <summary>
/// Singleton in-memory rate limiter for login and access-code attempts.
/// Tracks failures per key (IP + attempt type). After <see cref="MaxFailures"/>
/// failures within <see cref="WindowSeconds"/> seconds the key is locked out
/// for <see cref="LockoutSeconds"/> seconds.
/// </summary>
public class LoginRateLimiter
{
    private sealed record AttemptState(int FailCount, DateTime WindowStart, DateTime? LockedUntil);

    private readonly ConcurrentDictionary<string, AttemptState> _state = new();

    public const int MaxFailures    = 5;
    public const int WindowSeconds  = 60;   // window in which failures are counted
    public const int LockoutSeconds = 300;  // 5-minute lockout after MaxFailures

    /// <summary>Returns true if the key is currently locked out.</summary>
    public bool IsLocked(string key)
    {
        if (!_state.TryGetValue(key, out var s) || s.LockedUntil is null) return false;
        if (DateTime.UtcNow < s.LockedUntil) return true;
        _state.TryRemove(key, out _); // expired — clear
        return false;
    }

    /// <summary>How long until the lockout expires (zero if not locked).</summary>
    public TimeSpan LockoutRemaining(string key)
    {
        if (!_state.TryGetValue(key, out var s) || s.LockedUntil is null) return TimeSpan.Zero;
        var remaining = s.LockedUntil.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>Record a failed attempt; applies lockout if threshold is reached.</summary>
    public void RecordFailure(string key)
    {
        _state.AddOrUpdate(key,
            _ => new AttemptState(1, DateTime.UtcNow, null),
            (_, old) =>
            {
                var now      = DateTime.UtcNow;
                var inWindow = (now - old.WindowStart).TotalSeconds < WindowSeconds;
                var count    = inWindow ? old.FailCount + 1 : 1;
                var start    = inWindow ? old.WindowStart  : now;
                var locked   = count >= MaxFailures ? (DateTime?)now.AddSeconds(LockoutSeconds) : null;
                return new AttemptState(count, start, locked);
            });
    }

    /// <summary>Clear the failure record after a successful login.</summary>
    public void RecordSuccess(string key) => _state.TryRemove(key, out _);

    /// <summary>
    /// How many attempts remain before lockout (shown to the user as a hint).
    /// Returns 0 when already locked.
    /// </summary>
    public int AttemptsRemaining(string key)
    {
        if (IsLocked(key)) return 0;
        if (!_state.TryGetValue(key, out var s)) return MaxFailures;
        var inWindow = (DateTime.UtcNow - s.WindowStart).TotalSeconds < WindowSeconds;
        return inWindow ? Math.Max(0, MaxFailures - s.FailCount) : MaxFailures;
    }
}
