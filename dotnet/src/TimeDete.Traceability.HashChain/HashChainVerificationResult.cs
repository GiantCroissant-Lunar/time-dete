namespace TimeDete.Traceability.HashChain;

public sealed class HashChainVerificationResult
{
    public ChainKey ChainKey { get; init; }

    public bool IsValid { get; init; }

    public long EventsChecked { get; init; }

    public string? FirstCorruptedEventId { get; init; }

    public long? FirstCorruptedEventIndex { get; init; }

    public string? Message { get; init; }
}
