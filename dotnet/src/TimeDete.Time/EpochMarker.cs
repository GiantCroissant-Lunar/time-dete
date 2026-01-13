using Plate.TimeDete.Time.Primitives;

namespace Plate.TimeDete.Time;

/// <summary>
/// Describes a canonical epoch in the simulation timeline.
/// Epochs group ranges of canonical ticks into named eras.
/// </summary>
public sealed record EpochMarker
{
    /// <summary>
    /// Gets the canonical name of the epoch (for example, "Geological", "Biological").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the first tick that belongs to this epoch.
    /// </summary>
    public required CanonicalTick StartTick { get; init; }

    /// <summary>
    /// Gets the first tick after this epoch ends, if the epoch has a defined end.
    /// When <c>null</c>, the epoch is considered open-ended.
    /// </summary>
    public CanonicalTick? EndTickExclusive { get; init; }
}
