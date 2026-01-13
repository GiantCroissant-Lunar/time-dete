namespace Plate.TimeDete.Traceability.HashChain;

public readonly record struct BranchId(string Value);

public readonly record struct ChainPoint(
    ChainKey ChainKey,
    BranchId BranchId,
    string HeadHash);

public sealed record ChainHead(
    ChainKey ChainKey,
    BranchId BranchId,
    string HeadHash);

public sealed record BranchFork(
    BranchId ParentBranchId,
    string ForkPointHash);

public sealed record SnapshotAnchor(
    ChainKey ChainKey,
    BranchId BranchId,
    string HeadHash);

public sealed record SnapshotMetadata(
    SnapshotAnchor Anchor,
    string SnapshotHash,
    string SnapshotFormat,
    int SnapshotFormatVersion);

public sealed record HashChainRecordEnvelope<TPayload>(
    ChainKey ChainKey,
    BranchId BranchId,
    string WorldId,
    long CanonicalTick,
    string RecordType,
    TPayload Payload,
    string? PreviousHash);
