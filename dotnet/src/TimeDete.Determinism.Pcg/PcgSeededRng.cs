using Pcg;
using Plate.TimeDete.Determinism.Abstractions;

namespace Plate.TimeDete.Determinism.Pcg;

public sealed class PcgSeededRng : ISeededRng
{
    private Pcg32 _rng;
    private readonly ulong _seed;

    public PcgSeededRng(ulong seed)
    {
        _seed = seed;
        _rng = new Pcg32(seed, DeriveStream(seed));
    }

    public PcgSeededRng(RngState state)
    {
        _seed = state.State;
        _rng = new Pcg32(0, 0);
        SetState(state);
    }

    public ulong Seed => _seed;

    public uint NextUInt32() => _rng.Next();

    public ulong NextUInt64()
    {
        ulong high = _rng.Next();
        ulong low = _rng.Next();
        return (high << 32) | low;
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be positive");
        return (int)_rng.Next((uint)maxExclusive);
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than minInclusive");

        uint range = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)_rng.Next(range);
    }

    public float NextFloat()
    {
        const float scale = 1.0f / (1u << 24);
        return (_rng.Next() >> 8) * scale;
    }

    public double NextDouble()
    {
        // NOTE: Cannot use _rng.NextDouble() extension method directly because
        // Pcg32 is a struct and the extension method takes `this IPcgRng<uint>`,
        // which causes boxing. The boxed copy gets mutated, not _rng.
        // Instead, call _rng.Next() directly to advance state properly.
        const double scale = 1.0 / (uint.MaxValue + 1.0);
        return _rng.Next() * scale;
    }

    public bool NextBool() => (_rng.Next() & 1) == 1;

    public bool NextBool(float probability)
    {
        if (probability <= 0f) return false;
        if (probability >= 1f) return true;
        return NextFloat() < probability;
    }

    public ISeededRng Fork(ulong childSeed)
    {
        var currentState = GetState();
        var mixedSeed = MixSeeds(currentState.State, childSeed);
        return new PcgSeededRng(mixedSeed);
    }

    public RngState GetState() => new(_rng.State, _rng.Increment);

    public void SetState(RngState state)
    {
        var stream = state.Inc >> 1;
        _rng.SetStream(stream);
        _rng.State = state.State;
    }

    public void Advance(ulong steps) => _rng.Advance(steps);

    private static ulong DeriveStream(ulong seed)
    {
        var h = seed;
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccd;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53;
        h ^= h >> 33;
        return h;
    }

    private static ulong MixSeeds(ulong a, ulong b)
    {
        var s = a + b;
        s = (s ^ (s >> 30)) * 0xbf58476d1ce4e5b9;
        s = (s ^ (s >> 27)) * 0x94d049bb133111eb;
        return s ^ (s >> 31);
    }
}
