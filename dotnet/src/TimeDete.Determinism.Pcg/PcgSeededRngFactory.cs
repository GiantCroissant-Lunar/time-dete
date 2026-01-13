using Plate.TimeDete.Determinism.Abstractions;

namespace Plate.TimeDete.Determinism.Pcg;

public sealed class PcgSeededRngFactory : ISeededRngFactory
{
    private static readonly Random _systemRandom = new();

    public ISeededRng Create(ulong seed)
    {
        return new PcgSeededRng(seed);
    }

    public ISeededRng Create(RngState state)
    {
        return new PcgSeededRng(state);
    }

    public ISeededRng CreateRandom()
    {
        Span<byte> bytes = stackalloc byte[8];
        lock (_systemRandom)
        {
            _systemRandom.NextBytes(bytes);
        }

        var seed = BitConverter.ToUInt64(bytes);
        return new PcgSeededRng(seed);
    }
}
