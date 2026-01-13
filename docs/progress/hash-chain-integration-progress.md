# Hash-Chain Integration Progress (time-dete + sim-world + mung-bean)

## Status

- **time-dete**: Implemented core hash-chain primitives + branching metadata + envelope hashing. Tests passing.
- **sim-world**: Updated to per-domain chain verification and per-domain previous-hash chaining. Tests passing.
- **mung-bean**: Added a hash-chained ledger stream to session recordings and verification on replay load. Tests passing.

---

## time-dete (Plate.TimeDete)

### RFC updates

- **RFC**: `docs/rfcs/RFC-0004-hash-chain-traceability.md`
- Added formalization for:
  - **Branching/Forks** (append-only, fork-on-append-to-past)
  - **Snapshots anchored to chain heads** (checkpoint metadata)
  - **Unified timeline across scopes** (same time, different domains)
  - **Record envelope** as a consumer pattern (envelope is the hashed payload; chain link remains `PreviousHash`)

### Code changes

- **Project**: `dotnet/src/TimeDete.Traceability.HashChain/`
- Added value + metadata types:
  - `BranchId`, `ChainPoint`, `ChainHead`, `BranchFork`
  - `SnapshotAnchor`, `SnapshotMetadata`
  - `HashChainRecordEnvelope<TPayload>`

- Added hash-chain primitives:
  - `IHashChainHasher`
  - `HashChain` helper (`ComputeNextHash`, `VerifyLink`)
  - `Sha256HashChainHasher`
  - `CanonicalEnvelopeHashComputer`

- Updated existing verifier API to use `ChainKey`:
  - `HashChainVerificationResult.ChainKey` is now `ChainKey`
  - `HashChainVerifier.Verify(...)` now takes `ChainKey`

### Tests

- **Project**: `dotnet/tests/TimeDete.Traceability.HashChain.Tests/`
- Added coverage for:
  - `Sha256HashChainHasher`
  - `HashChain.VerifyLink`
  - `CanonicalEnvelopeHashComputer` (golden canonical JSON string)

---

## sim-world adoption

### Goal

Align sim-world with the **per-domain chain** model: each `(worldId, domainLayer)` is its own chain.

### Key fixes

- **Per-domain verification**
  - Updated sim-world’s hash-chain verifier wrapper to pass `ChainKey = "{worldId}:{domainLayer}"` into TimeDete `HashChainVerifier`.
  - Updated all call sites to verify by domain groups.

- **Per-domain previous-hash chaining (critical)**
  - Fixed `EventStore.AppendEvent(...)` to compute `PreviousHash` from the last event in the same `(worldId, domainLayer)` chain.
  - Resolved ordering edge cases by selecting “last” using `OrderBy(Tick).ThenBy(RecordedAt).LastOrDefault()`.

### Key files

- `project/plugins/SimWorld.EventSourcing/EventStore.cs`
- `project/plugins/SimWorld.EventSourcing/HashChainVerifier.cs`
- `project/plugins/SimWorld.Core/Verification/ChainVerificationService.cs`
- `project/plugins/SimWorld.Storage.*.ArangoDB/.../ArangoEventStore.cs`
- `project/hosts/SimWorld.Window.Avalonia/.../InMemoryEventStore.cs`

### Tests updated

- `project/tests/SimWorld.Tests.Unit/EventSourcing/HashChainVerifierTests.cs`
- `project/tests/SimWorld.Tests.Oracle/EventReplayTests.cs`
  - Fixed oracle in-memory repo to respect query filters (especially `DomainLayer`).

---

## mung-bean adoption

### Goal

Add tamper-evident hash-chain verification to session recordings with minimal schema disruption.

### Recording ledger

- Added `ledger` NDJSON entries as extra lines in session recordings.
- Ledger lines hash the canonical JSON representation of the event line.
- Footer remains **last line** (summary readers remain compatible).

### Key files

- `project/contracts/MungBean.Recording/Events.cs`
  - Added `LedgerEvent`

- `project/plugins/MungBean.Recording.Core/AsyncSessionRecorder.cs`
  - Writes `ledger` entries:
    - after `header`
    - after each event
    - immediately before `footer` (footer remains last)

- `project/plugins/MungBean.Recording.Core/LedgerChainVerifier.cs`
  - Verifies:
    - event -> ledger pairing
    - previous-hash linkage
    - recomputed hash matches stored hash
    - footer is last non-empty record when ledger exists
  - Backward compatible: if no ledger entries exist, returns valid with message.

- `project/plugins/MungBean.Replay.Core/ReplayEngine.cs`
  - Calls `LedgerChainVerifier.VerifyAsync(...)` on `LoadAsync`.

- `project/plugins/MungBean.Recording.Core/SessionPlayer.cs`
  - Deserializes `snapshot`, `rng_state`, `ledger` event types.

### Build/test

- `dotnet test MungBean.SkiaSharp.sln -c Release` passed.

---

## Pending / Next steps

- **Decision (mung-bean)**: Option A
  - Keep a **global** session ledger chain (total ordering of the recording).
  - Optionally add **per-stream** chain pointers (stream-local `PreviousHash`) for:
    - `{sessionId}:inputs`
    - `{sessionId}:snapshots`
    - `{sessionId}:rng_state`
    - `{sessionId}:system`

- Define explicit **macro/micro anchoring contract**:
  - `(SimWorld.WorldId, BranchId, CanonicalTick)` anchor stored in mung-bean session header or a dedicated event.
  - Decision: include a **set of domain chain heads** (all domains) at or before `CanonicalTick`.
  - Decision: require **per-domain init events** so domain chains are never empty.

- Decide snapshot strategy for sim-world:
  - snapshots as projection checkpoints anchored to chain head hash.

### Mung-bean: chain partitioning

- **Default recommendation**
  - Keep a **single** session ledger chain initially (simplest, least schema disruption).
  - Add a `Scope`/`Stream` field on ledger entries (or derive from the event type) so future multi-chain can be introduced without rethinking the recording format.

- **Decision trigger** (when to split)
  - Split into multiple chains only if you need:
    - independent verification/partial loading per stream, or
    - different canonical ordering rules per stream.

### Macro/micro anchoring contract (sim-world ↔ mung-bean)

- **Goal**
  - Be able to prove “this mung-bean session replay corresponds to *this* sim-world chain head (or tick range)”.

- **Context**
  - sim-world is the macro-scale world generator/simulator (map-like, zoom/LOD, cultures/languages/calendars).
  - mung-bean is the micro-scale adventure runtime (dungeons/caves/villages, NPCs/monsters, player/items).
  - Both are one timeline: mung-bean sessions should be anchored into sim-world’s world/time so player actions advance the world.

- **Proposed minimal fields**
  - `WorldId`
  - `BranchId`
  - `CanonicalTick`
  - `DomainHeads` (all domains; vector at or before `CanonicalTick`)
    - `DomainLayer`
    - `HeadHash`
    - `HeadTick` (last event tick in that domain at or before `CanonicalTick`)

- **Where to store it**
  - Prefer a **dedicated recording event** (rather than only a header field) so the anchor itself is included in the ledger chain.

### sim-world snapshot strategy

- **Default recommendation**
  - Treat snapshots as **projection checkpoints** (not primary history).
  - Store snapshot metadata that includes:
    - `ChainKey`
    - `ChainHeadHash`
    - `Tick`
    - optional `ProjectionName`/`Version`

### Checklist

- [x] Decide mung-bean: Option A (global chain + optional per-stream pointers).
- [x] Decide anchoring: include **all domain** chain heads at session start.
- [x] Decide `DomainHeads` shape: Option 2 (`DomainLayer` + `HeadHash` + `HeadTick`).
- [x] Decide empty-domain handling: require per-domain init events (no empty chains).
- [ ] Implement the chosen anchoring event/header contract and include it in verification.
- [ ] Define sim-world snapshot metadata schema + where it lives (event store vs side store).
- [ ] Add a cross-project “golden” test that ensures canonical JSON hashing is stable for at least one representative payload.
