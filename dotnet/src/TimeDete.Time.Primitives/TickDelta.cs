using System;

namespace Plate.TimeDete.Time.Primitives;

public readonly record struct TickDelta : IComparable<TickDelta>
{
    public long Value { get; }

    public TickDelta(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Tick delta must be non-negative.");
        Value = value;
    }

    public static readonly TickDelta Zero = new(0);

    public static readonly TickDelta One = new(1);

    public static TickDelta FromLong(long value) => new(value);

    public int CompareTo(TickDelta other) => Value.CompareTo(other.Value);

    public override string ToString() => $"Δ{Value}";

    public static explicit operator TickDelta(long value) => new(value);

    public static explicit operator long(TickDelta delta) => delta.Value;

    public static TickDelta operator +(TickDelta left, TickDelta right) => new(checked(left.Value + right.Value));

    public static TickDelta operator -(TickDelta left, TickDelta right)
    {
        var result = left.Value - right.Value;
        if (result < 0)
            throw new ArgumentOutOfRangeException(nameof(right), "Resulting tick delta must be non-negative.");
        return new(result);
    }
}
