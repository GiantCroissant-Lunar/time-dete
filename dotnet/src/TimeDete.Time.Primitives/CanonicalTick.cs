using System;

namespace Plate.TimeDete.Time.Primitives;

/// <summary>
/// Represents an absolute point in simulation time as an immutable value.
/// CanonicalTick is the substrate time unit that exists independent of any cultural calendar system.
/// </summary>
/// <remarks>
/// All simulation events are timestamped with CanonicalTick values. Cultural calendars
/// project CanonicalTick values into human-readable dates (e.g., "Year 42 of the Third Age").
/// Tick 0 represents the genesis moment when the world simulation begins.
/// </remarks>
/// <param name="Value">The absolute tick value (0 = genesis, positive = simulation time).</param>
/// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
public readonly record struct CanonicalTick : IComparable<CanonicalTick>
{
    /// <summary>
    /// The absolute tick value (must be non-negative).
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Initializes a new CanonicalTick.
    /// </summary>
    /// <param name="value">The tick value (must be non-negative).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
    public CanonicalTick(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Tick value must be non-negative.");
        Value = value;
    }

    /// <summary>
    /// The genesis tick representing the beginning of simulation time (tick 0).
    /// </summary>
    public static readonly CanonicalTick Genesis = new(0);

    /// <summary>
    /// Creates a CanonicalTick from a long value.
    /// </summary>
    /// <param name="value">The tick value. Must be non-negative.</param>
    /// <returns>A new CanonicalTick.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
    public static CanonicalTick FromLong(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
        return new(value);
    }

    /// <summary>
    /// Returns the next tick in the simulation (current + 1).
    /// </summary>
    /// <returns>A new CanonicalTick representing the next simulation step.</returns>
    public CanonicalTick Next() => new(Value + 1);

    public CanonicalTick Add(TickDelta delta)
    {
        return new(Value + delta.Value);
    }

    /// <summary>
    /// Adds a delta to the current tick.
    /// </summary>
    /// <param name="delta">The number of ticks to add. Must be non-negative.</param>
    /// <returns>A new CanonicalTick with the added value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delta is negative.</exception>
    public CanonicalTick Add(long delta)
    {
        if (delta < 0)
            throw new ArgumentOutOfRangeException(nameof(delta), "Delta must be non-negative.");
        return new(Value + delta);
    }

    public CanonicalTick Subtract(TickDelta delta)
    {
        return Subtract(delta.Value);
    }

    /// <summary>
    /// Subtracts a delta from the current tick.
    /// </summary>
    /// <param name="delta">The number of ticks to subtract. Must be non-negative.</param>
    /// <returns>A new CanonicalTick with the subtracted value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delta is negative or result would be negative.</exception>
    public CanonicalTick Subtract(long delta)
    {
        if (delta < 0)
            throw new ArgumentOutOfRangeException(nameof(delta), "Delta must be non-negative.");

        var result = Value - delta;
        if (result < 0)
            throw new ArgumentOutOfRangeException(nameof(delta), "Resulting tick value must be non-negative.");

        return new(result);
    }

    /// <summary>
    /// Compares this tick to another for ordering.
    /// </summary>
    /// <param name="other">The other tick to compare to.</param>
    /// <returns>Negative if this &lt; other, 0 if equal, positive if this &gt; other.</returns>
    public int CompareTo(CanonicalTick other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Returns a string representation of this tick.
    /// </summary>
    /// <returns>A string in the format "Tick {Value}".</returns>
    public override string ToString() => $"Tick {Value}";

    // Comparison operators
    /// <summary>Determines if the left tick is less than the right tick.</summary>
    public static bool operator <(CanonicalTick left, CanonicalTick right) => left.Value < right.Value;

    /// <summary>Determines if the left tick is greater than the right tick.</summary>
    public static bool operator >(CanonicalTick left, CanonicalTick right) => left.Value > right.Value;

    /// <summary>Determines if the left tick is less than or equal to the right tick.</summary>
    public static bool operator <=(CanonicalTick left, CanonicalTick right) => left.Value <= right.Value;

    /// <summary>Determines if the left tick is greater than or equal to the right tick.</summary>
    public static bool operator >=(CanonicalTick left, CanonicalTick right) => left.Value >= right.Value;

    // Arithmetic operators
    /// <summary>Adds a delta to a tick.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delta is negative.</exception>
    public static CanonicalTick operator +(CanonicalTick tick, long delta) => tick.Add(delta);

    public static CanonicalTick operator +(CanonicalTick tick, TickDelta delta) => tick.Add(delta);

    /// <summary>Subtracts a delta from a tick.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when delta is negative or result would be negative.</exception>
    public static CanonicalTick operator -(CanonicalTick tick, long delta) => tick.Subtract(delta);

    public static CanonicalTick operator -(CanonicalTick tick, TickDelta delta) => tick.Subtract(delta);

    /// <summary>Returns the difference between two ticks.</summary>
    public static long operator -(CanonicalTick left, CanonicalTick right) => left.Value - right.Value;
}
