using System;
using Plate.TimeDete.Time.Primitives;

namespace Plate.TimeDete.Time.Tests;

public sealed class CanonicalTickTests
{
    [Fact]
    public void Ctor_NegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CanonicalTick(-1));
    }

    [Fact]
    public void FromLong_NonNegative_Succeeds()
    {
        var tick = CanonicalTick.FromLong(10);
        Assert.Equal(10, tick.Value);
    }

    [Fact]
    public void Addition_NegativeDelta_Throws()
    {
        var tick = new CanonicalTick(5);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = tick + (-1));
    }

    [Fact]
    public void Addition_IncreasesValue()
    {
        var tick = new CanonicalTick(5);
        var result = tick + 3;
        Assert.Equal(8, result.Value);
    }

    [Fact]
    public void Subtraction_NegativeDelta_Throws()
    {
        var tick = new CanonicalTick(5);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = tick - (-1));
    }

    [Fact]
    public void Subtraction_ResultingNegative_Throws()
    {
        var tick = new CanonicalTick(5);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = tick - 10);
    }

    [Fact]
    public void Subtraction_DecreasesValue()
    {
        var tick = new CanonicalTick(5);
        var result = tick - 3;
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public void DifferenceBetweenTicks_IsSigned()
    {
        var t1 = new CanonicalTick(10);
        var t2 = new CanonicalTick(4);

        Assert.Equal(6, t1 - t2);
        Assert.Equal(-6, t2 - t1);
    }

    [Fact]
    public void ComparisonOperators_WorkAsExpected()
    {
        var smaller = new CanonicalTick(1);
        var larger = new CanonicalTick(2);

        Assert.True(smaller < larger);
        Assert.True(larger > smaller);
        Assert.True(smaller <= larger);
        Assert.True(larger >= smaller);
        Assert.True(smaller == new CanonicalTick(1));
        Assert.True(smaller != larger);
    }
}

public sealed class TickScaleTests
{
    [Fact]
    public void Ctor_WithNonPositiveNumerator_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickScale(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickScale(-1, 1));
    }

    [Fact]
    public void Ctor_WithNonPositiveDenominator_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickScale(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickScale(1, -1));
    }

    [Fact]
    public void Scale_UsesIntegerDivisionFloorSemantics()
    {
        var scale = new TickScale(1, 60);
        Assert.Equal(0, scale.Scale(0));
        Assert.Equal(0, scale.Scale(1));
        Assert.Equal(1, scale.Scale(60));
        Assert.Equal(2, scale.Scale(120));
        Assert.Equal(2, scale.Scale(179));
    }
}

public sealed class TickMappingTests
{
    [Fact]
    public void FixedAnchorMode_IgnoresSourceTick()
    {
        var mapping = new TickMapping(TickMappingMode.FixedAnchor);
        var anchor = new CanonicalTick(123);

        Assert.Equal(new CanonicalTick(123), mapping.Map(anchor, 0));
        Assert.Equal(new CanonicalTick(123), mapping.Map(anchor, 999));
    }

    [Fact]
    public void AdvancingMode_AppliesScale()
    {
        var mapping = new TickMapping(TickMappingMode.Advancing, new TickScale(1, 60));
        var anchor = new CanonicalTick(1000);

        Assert.Equal(new CanonicalTick(1000), mapping.Map(anchor, 0));
        Assert.Equal(new CanonicalTick(1000), mapping.Map(anchor, 59));
        Assert.Equal(new CanonicalTick(1001), mapping.Map(anchor, 60));
        Assert.Equal(new CanonicalTick(1002), mapping.Map(anchor, 120));
    }

    [Fact]
    public void TryParseMode_RecognizesKnownStrings()
    {
        Assert.True(TickMapping.TryParseMode("fixed_anchor", out var fixedMode));
        Assert.Equal(TickMappingMode.FixedAnchor, fixedMode);

        Assert.True(TickMapping.TryParseMode("advancing", out var advancingMode));
        Assert.Equal(TickMappingMode.Advancing, advancingMode);
    }
}