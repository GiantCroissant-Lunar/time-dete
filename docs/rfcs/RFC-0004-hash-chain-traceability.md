# RFC-0004: Hash-Chain Traceability (Local Blockchain)

## Status: Draft

## Created: 2025-12-12

## Depends On: RFC-0001, RFC-0002

---

## 1. Overview

This RFC proposes a reusable **hash-chain traceability** module under the **time-dete** repository.

This is *not* a distributed blockchain. It is a deterministic, local, append-only **hash chain** that provides:

- Tamper evidence
- Verifiable ordering
- Lightweight “proof of integrity” for recorded histories

Primary target consumers:

- **sim-world**: per-domain (worldId, domainLayer) event chains
- **mung-bean**: run/session ledgers (inputs, key events, checkpoints)

---

## 2. Problem Statement

Both sim-world and mung-bean need a way to:

- Detect accidental corruption or intentional tampering
- Provide a stable “run/world fingerprint” for verification
- Verify that a recorded timeline is complete and ordered

sim-world already uses a hash-chain concept (RFC-002). mung-bean can use the same concept for:

- replay validation
- speedrun verification
- save/run tamper detection
- multiplayer/coop desync detection (compare chain heads)

---

## 3. Goals

- Provide a **small**, reusable core API that can hash-chain arbitrary records.
- Provide deterministic hashing helpers so hashes are stable across:
  - platforms
  - runtimes
  - serialization order
- Avoid coupling to:
  - sim-world event types
  - mung-bean input schemas
  - storage backends

---

## 4. Non-Goals

- No distributed consensus, mining, networking.
- No storage layer. Persistence is handled by consumers.
- No hard dependency on JSON as a payload format.

---

## 5. Proposed Package Layout

- `Plate.TimeDete.Traceability.HashChain`
  - Hash computation primitives
  - Canonicalization helpers (optional)
  - Verification helpers

---

## 6. Core Concepts

### 6.1 Chain Key (Multi-Chain)

A single application commonly maintains multiple independent chains.

Examples:

- sim-world: `(worldId, domainLayer)`
- mung-bean: `(runId, streamName)` where `streamName` might be `inputs`, `events`, `checkpoints`

We represent this as an opaque identifier:

```csharp
public readonly record struct ChainKey(string Value);
```

### 6.2 Hash Algorithm

- Use SHA-256 for stable, widely available hashing.
- Hash output is hex lowercase.

### 6.3 Branching (Forks) and “Time Travel” (Consumer Pattern)

Hash chains are append-only. If you want to “go back in time and keep going” you must **fork**.

- Forking creates a **new branch** starting from a historical point (`forkPointHash`) in an existing branch.
- The new branch’s first appended record uses `previousHash = forkPointHash`.
- The existing branch is unchanged.

This RFC does not require consumers to implement branching, but it standardizes the terms and recommended metadata.

```csharp
public readonly record struct BranchId(string Value);

/// Identifies a specific historical point within a chain.
public readonly record struct ChainPoint(
    ChainKey ChainKey,
    BranchId BranchId,
    string HeadHash);

/// Describes how a branch was created (optional but recommended for traceability).
public sealed record BranchFork(
    BranchId ParentBranchId,
    string ForkPointHash);
```

Recommended fork rule:

- If you attempt to append to a historical point that is not the current branch head, the system SHOULD create a new `BranchId` and record a `BranchFork` linking it to the parent branch and fork point.

---

## 7. API Sketch (Illustrative)

### 7.1 Hash-chain primitives

```csharp
public interface IHashChainHasher
{
    string ComputeHash(ReadOnlySpan<byte> payload, string? previousHash);
}

public static class HashChain
{
    public static string ComputeNextHash(
        IHashChainHasher hasher,
        ReadOnlySpan<byte> payload,
        string? previousHash);

    public static bool VerifyLink(
        IHashChainHasher hasher,
        ReadOnlySpan<byte> payload,
        string? previousHash,
        string expectedHash);
}
```

### 7.2 Canonical payload helpers (optional)

Consumers will often have an “object payload” and want stable bytes.

We can provide optional helpers:

- `CanonicalJson` (sorted keys, compact)
- `CanonicalUtf8` helpers

These MUST be versioned carefully because changing canonicalization is a breaking change.

### 7.3 Record envelope (consumer pattern, optional)

Different scopes/domains will have different payload shapes (culture update vs dungeon generation vs player input).
To make verification and “same time across scopes” unambiguous, consumers may define a standard **envelope** that wraps the payload with shared metadata.

The hash-chain library does not require this type, but it is a recommended consumer pattern.

The envelope does not replace the hash-chain “local blockchain” concept.
It is intended to define the canonical payload bytes that the chain hashes and verifies, while `PreviousHash` remains the explicit link between records.

```csharp
/// Illustrative only.
/// This is not a required library type; consumers decide exact fields and types.
public sealed record HashChainRecordEnvelope(
    ChainKey ChainKey,
    BranchId BranchId,
    string WorldId,
    long CanonicalTick,
    string RecordType,
    object Payload,
    string? PreviousHash);
```

Recommended hashing guidance:

- Hash the canonical bytes of the envelope fields that define the record identity and ordering:
  - `WorldId`, `BranchId`, `CanonicalTick`, `RecordType`, and `Payload`.
- Use `PreviousHash` as the chain link input to the hasher (i.e. do not rely on embedding `PreviousHash` inside the payload to establish the link).

This yields two important properties:

- Different chains (different `ChainKey`) can still be aligned on the same `(WorldId, BranchId, CanonicalTick)`.
- Forking semantics remain clear: the fork point is identified by a `ForkPointHash`, and the new branch continues by using that hash as `PreviousHash`.

---

## 8. Integration Patterns

### 8.1 sim-world (Event sourcing)

- Each `WorldEvent` already stores:
  - `PreviousHash`
  - `Hash`

The shared library should NOT define `WorldEvent`, but it can:

- provide `HashChainHasherSha256`
- provide canonical JSON sorting utilities used by sim-world

Multi-chain usage:

- `ChainKey = $"{worldId}:{domainLayer}"`

### 8.2 mung-bean (Run ledger)

Maintain a chain for a run.

Events to chain could include:

- input frames
- RNG master seed announcement
- checkpoint snapshots (hash only, not full snapshot data)
- key gameplay events (boss killed, floor cleared)

Multi-chain usage:

- `ChainKey = $"{runId}:inputs"`
- `ChainKey = $"{runId}:events"`

### 8.3 Snapshots (Checkpoints) anchored to the chain head

Snapshots are performance optimizations (projection checkpoints). They are not authoritative history.

A snapshot SHOULD be anchored to a chain head so it can be verified and replay can resume from the correct point.

```csharp
public sealed record SnapshotAnchor(
    ChainKey ChainKey,
    BranchId BranchId,
    string HeadHash);

public sealed record SnapshotMetadata(
    SnapshotAnchor Anchor,
    string SnapshotHash,
    string SnapshotFormat,
    int SnapshotFormatVersion);
```

Notes:

- `SnapshotHash` should be computed over the snapshot payload (or over a canonical form of it).
- `SnapshotFormat`/`SnapshotFormatVersion` exist so consumers can invalidate snapshots when the format or projection logic changes.

### 8.4 Unified timeline across scopes (sim-world + mung-bean)

“Macro” (sim-world) and “micro” (mung-bean) are different **scopes** of the same world at the same time.
In practice this means:

- Both scopes share a single **world identity** (`WorldId`).
- Both scopes share a single **branch identity** (`BranchId`) when you time-travel and fork.
- Both scopes share a single **time coordinate** (e.g. `CanonicalTick`) so that calendar, culture, terrain, and town/player state are all interpreted at the same time.

Recommended pattern:

- Store a time coordinate in each consumer record (or include it in the payload being hashed).
- Keep separate chains per domain/scope (inputs, player-state, culture, climate, etc.) by encoding the scope into `ChainKey`.

Example chain key composition:

```csharp
// Illustrative only; consumers decide their formatting.
// The important part is that WorldId + BranchId are present.
// Domain/scope identifies what is being chained.
// CanonicalTick belongs in the record payload (or additional indexing), not the key.
var chainKey = new ChainKey($"{worldId}:{branchId}:{domainScope}");
```

Interpretation rule:

- When the player is in a town at time `T`, all projections (map/culture/calendar/player-state) must be loaded and queried “as-of `T`” on the same `WorldId` and `BranchId`.

---

## 9. Determinism Requirements

- Hashing must be deterministic.
- Canonicalization (if provided) must be deterministic.
- Chain verification must be stable across platforms.

---

## 10. Migration Notes

- sim-world can gradually replace internal hashing utilities with `Plate.TimeDete.Traceability.HashChain`.
- mung-bean can adopt run-ledger hashing without changing gameplay logic.

---

## 11. Open Questions

- Do we need a formal “chain head” record type, separate from `ChainPoint`?

  Example:

  ```csharp
  public sealed record ChainHead(
      ChainKey ChainKey,
      BranchId BranchId,
      string HeadHash);
  ```

- Should we provide a built-in monotonic sequence number helper, or leave ordering to consumers?
- Should canonical JSON live in this package, or in a sibling package (e.g., `Plate.TimeDete.Serialization.CanonicalJson`)?
