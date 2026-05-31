using System.Text;
using TimeDete.Traceability.Hashing;

namespace TimeDete.Traceability.HashChain;

public sealed class Sha256HashChainHasher : IHashChainHasher
{
    public string ComputeHash(ReadOnlySpan<byte> payload, string? previousHash)
    {
        var previous = previousHash ?? string.Empty;
        var previousBytes = Encoding.UTF8.GetBytes(previous);

        var combined = new byte[previousBytes.Length + payload.Length];
        previousBytes.CopyTo(combined, 0);
        payload.CopyTo(combined.AsSpan(previousBytes.Length));

        return Sha256Hex.ComputeLowerHex(combined);
    }
}
