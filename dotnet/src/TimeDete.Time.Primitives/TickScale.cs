using System;

namespace Plate.TimeDete.Time.Primitives;

public readonly record struct TickScale
{
    public long Numerator { get; }

    public long Denominator { get; }

    public TickScale(long numerator, long denominator)
    {
        if (numerator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numerator), "Numerator must be positive.");
        }

        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), "Denominator must be positive.");
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    public static TickScale Identity { get; } = new(1, 1);

    public long Scale(long value)
    {
        return checked(value * Numerator / Denominator);
    }

    public TickScale Invert()
    {
        return new TickScale(Denominator, Numerator);
    }
}
