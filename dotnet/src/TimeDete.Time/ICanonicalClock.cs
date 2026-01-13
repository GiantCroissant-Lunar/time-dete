using Plate.TimeDete.Time.Primitives;

namespace Plate.TimeDete.Time;

/// <summary>
/// Provides access to the canonical simulation tick and current epoch.
/// Implementations must guarantee monotonic, non-decreasing tick progression.
/// </summary>
public interface ICanonicalClock
{
    /// <summary>
    /// Gets the current canonical tick.
    /// </summary>
    CanonicalTick CurrentTick { get; }

    /// <summary>
    /// Gets the current epoch marker for the simulation timeline.
    /// </summary>
    EpochMarker CurrentEpoch { get; }

    /// <summary>
    /// Advances the canonical clock by the specified number of ticks.
    /// </summary>
    /// <param name="ticks">The number of ticks to advance. Must be non-negative.</param>
    void Advance(long ticks = 1);
}
