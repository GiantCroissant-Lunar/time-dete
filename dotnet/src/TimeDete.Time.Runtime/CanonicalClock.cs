using System;
using System.Threading;
using TimeDete.Time;
using TimeDete.Time.Primitives;

namespace TimeDete.Time.Runtime;

/// <summary>
/// Default implementation of <see cref="ICanonicalClock"/> that provides a
/// monotonic, thread-safe canonical tick counter and current epoch marker.
/// </summary>
public sealed class CanonicalClock : ICanonicalClock
{
    private long _currentTick;
    private EpochMarker _currentEpoch;

    /// <summary>
    /// Initializes a new instance of the <see cref="CanonicalClock"/> class.
    /// </summary>
    /// <param name="initialTick">Initial tick value. Must be non-negative.</param>
    /// <param name="initialEpoch">Initial epoch marker.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialTick"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialEpoch"/> is <c>null</c>.</exception>
    public CanonicalClock(long initialTick, EpochMarker initialEpoch)
    {
        if (initialTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialTick), "Initial tick must be non-negative.");
        }

        _currentTick = initialTick;
        _currentEpoch = initialEpoch ?? throw new ArgumentNullException(nameof(initialEpoch));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CanonicalClock"/> class with tick 0
    /// and a default epoch that starts at tick 0 and has no defined end.
    /// </summary>
    public CanonicalClock()
        : this(0, new EpochMarker
        {
            Name = "Default",
            StartTick = new CanonicalTick(0),
            EndTickExclusive = null
        })
    {
    }

    /// <inheritdoc />
    public CanonicalTick CurrentTick => new(Interlocked.Read(ref _currentTick));

    /// <inheritdoc />
    public EpochMarker CurrentEpoch => _currentEpoch;

    /// <inheritdoc />
    public void Advance(long ticks = 1)
    {
        if (ticks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks), "Ticks to advance must be non-negative.");
        }

        if (ticks == 0)
        {
            return;
        }

        Interlocked.Add(ref _currentTick, ticks);
    }

    /// <summary>
    /// Updates the current epoch marker.
    /// </summary>
    /// <param name="epoch">The new epoch marker.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="epoch"/> is <c>null</c>.</exception>
    public void SetEpoch(EpochMarker epoch)
    {
        _currentEpoch = epoch ?? throw new ArgumentNullException(nameof(epoch));
    }
}
