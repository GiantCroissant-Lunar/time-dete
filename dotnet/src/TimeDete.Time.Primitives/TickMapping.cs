using System;

namespace Plate.TimeDete.Time.Primitives;

public readonly record struct TickMapping
{
    public TickMappingMode Mode { get; }

    public TickScale Scale { get; }

    public TickMapping(TickMappingMode mode, TickScale? scale = null)
    {
        Mode = mode;
        Scale = scale ?? TickScale.Identity;
    }

    public long MapToLong(long anchorTick, long sourceTick)
    {
        return Mode switch
        {
            TickMappingMode.FixedAnchor => anchorTick,
            TickMappingMode.Advancing => checked(anchorTick + Scale.Scale(sourceTick)),
            _ => checked(anchorTick + sourceTick)
        };
    }

    public CanonicalTick Map(CanonicalTick anchorTick, long sourceTick)
    {
        return new CanonicalTick(MapToLong(anchorTick.Value, sourceTick));
    }

    public static bool TryParseMode(string? text, out TickMappingMode mode)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            mode = TickMappingMode.Advancing;
            return false;
        }

        if (string.Equals(text, "fixed_anchor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "fixed-anchor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "fixed", StringComparison.OrdinalIgnoreCase))
        {
            mode = TickMappingMode.FixedAnchor;
            return true;
        }

        if (string.Equals(text, "advancing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "advance", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "scaled", StringComparison.OrdinalIgnoreCase))
        {
            mode = TickMappingMode.Advancing;
            return true;
        }

        mode = TickMappingMode.Advancing;
        return false;
    }
}
