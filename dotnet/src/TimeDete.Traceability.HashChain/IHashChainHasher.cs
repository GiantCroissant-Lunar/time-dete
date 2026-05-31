namespace TimeDete.Traceability.HashChain;

public interface IHashChainHasher
{
    string ComputeHash(ReadOnlySpan<byte> payload, string? previousHash);
}
