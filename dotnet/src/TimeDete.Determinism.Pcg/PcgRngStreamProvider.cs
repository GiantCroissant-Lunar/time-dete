using System.Collections.Concurrent;
using TimeDete.Determinism.Abstractions;

namespace TimeDete.Determinism.Pcg;

public sealed class PcgRngStreamProvider : IRngStreamProvider
{
    private readonly ConcurrentDictionary<string, ISeededRng> _streams = new();
    private readonly ISeededRngFactory _factory;
    private ulong _masterSeed;

    public PcgRngStreamProvider(ulong masterSeed)
        : this(masterSeed, new PcgSeededRngFactory())
    {
    }

    public PcgRngStreamProvider(ulong masterSeed, ISeededRngFactory factory)
    {
        _masterSeed = masterSeed;
        _factory = factory;
    }

    public ulong MasterSeed => _masterSeed;

    public ISeededRng GetStream(string name)
    {
#if NETSTANDARD2_1
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
#else
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
#endif

        return _streams.GetOrAdd(name, CreateStream);
    }

    public bool HasStream(string name)
    {
        return _streams.ContainsKey(name);
    }

    public IReadOnlyCollection<string> GetStreamNames()
    {
        return _streams.Keys.ToList().AsReadOnly();
    }

    public IReadOnlyDictionary<string, RngState> CaptureState()
    {
        var states = new Dictionary<string, RngState>();
        foreach (var kvp in _streams)
        {
            states[kvp.Key] = kvp.Value.GetState();
        }

        return states;
    }

    public void RestoreState(IReadOnlyDictionary<string, RngState> states)
    {
        _streams.Clear();

        foreach (var kvp in states)
        {
            var rng = _factory.Create(kvp.Value);
            _streams[kvp.Key] = rng;
        }
    }

    public void Reset(ulong masterSeed)
    {
        _masterSeed = masterSeed;
        _streams.Clear();
    }

    private ISeededRng CreateStream(string name)
    {
        var streamSeed = DeriveStreamSeed(_masterSeed, name);
        return _factory.Create(streamSeed);
    }

    private static ulong DeriveStreamSeed(ulong masterSeed, string name)
    {
        const ulong fnvPrime = 0x100000001b3;
        const ulong fnvOffset = 0xcbf29ce484222325;

        var hash = fnvOffset;
        foreach (var c in name)
        {
            hash ^= c;
            hash *= fnvPrime;
        }

        var combined = masterSeed ^ hash;
        combined = (combined ^ (combined >> 30)) * 0xbf58476d1ce4e5b9;
        combined = (combined ^ (combined >> 27)) * 0x94d049bb133111eb;
        return combined ^ (combined >> 31);
    }
}
