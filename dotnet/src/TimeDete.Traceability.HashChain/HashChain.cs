namespace TimeDete.Traceability.HashChain;

public static class HashChain
{
    public static string ComputeNextHash(
        IHashChainHasher hasher,
        ReadOnlySpan<byte> payload,
        string? previousHash)
    {
        if (hasher is null)
        {
            throw new ArgumentNullException(nameof(hasher));
        }

        return hasher.ComputeHash(payload, previousHash);
    }

    public static bool VerifyLink(
        IHashChainHasher hasher,
        ReadOnlySpan<byte> payload,
        string? previousHash,
        string expectedHash)
    {
        if (hasher is null)
        {
            throw new ArgumentNullException(nameof(hasher));
        }

        if (expectedHash is null)
        {
            throw new ArgumentNullException(nameof(expectedHash));
        }

        var computed = hasher.ComputeHash(payload, previousHash);
        return string.Equals(computed, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
