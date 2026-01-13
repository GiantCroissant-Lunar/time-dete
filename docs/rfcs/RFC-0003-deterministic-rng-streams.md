# RFC-0003: Deterministic RNG & RNG Streams

## Status: Draft

## Created: 2025-12-12

## Depends On: RFC-0001, RFC-0002

---

## 1. Overview

This RFC defines deterministic random foundations for `Plate.TimeDete`.

Goals:

- A deterministic RNG interface (`ISeededRng`)
- A stable, replay-friendly RNG state format (`RngState`)
- A named stream provider (`IRngStreamProvider`) for independent streams

This is primarily motivated by:

- sim-world needing deterministic generation/simulation suitable for debugging and replay
- mung-bean needing deterministic gameplay/snapshots

---

## 2. Design Principles

- Simulation code should avoid `System.Random`.
- Deterministic RNG should be accessed via:
  - a *named stream* per subsystem, or
  - a *derived seed per operation/event* (when event-sourced replay needs to be resilient to call-order changes).

---

## 3. Core Types

### 3.1 RngState

A small, serializable state payload.

```csharp
public readonly record struct RngState(ulong State, ulong Inc);
```

### 3.2 ISeededRng

```csharp
public interface ISeededRng
{
    ulong Seed { get; }

    uint NextUInt32();
    ulong NextUInt64();

    int Next(int maxExclusive);
    int Next(int minInclusive, int maxExclusive);

    float NextFloat();
    double NextDouble();

    bool NextBool();
    bool NextBool(float probability);

    ISeededRng Fork(ulong childSeed);

    RngState GetState();
    void SetState(RngState state);

    void Advance(ulong steps);
}
```

### 3.3 IRngStreamProvider

```csharp
public interface IRngStreamProvider
{
    ulong MasterSeed { get; }

    ISeededRng GetStream(string name);

    bool HasStream(string name);
    IReadOnlyCollection<string> GetStreamNames();

    IReadOnlyDictionary<string, RngState> CaptureState();
    void RestoreState(IReadOnlyDictionary<string, RngState> states);

    void Reset(ulong masterSeed);
}
```

---

## 4. Stream Naming Conventions

To reduce collisions and encourage stability, use names like:

- `world:{worldId}/spatial.subdivision`
- `world:{worldId}/language.soundchange`
- `world:{worldId}/settlement.growth`

Rules:

- Stream names must be stable across versions.
- Renaming a stream is effectively a breaking determinism change.

---

## 5. Implementation Strategy

### 5.1 Algorithm

Recommended default implementation: **PCG32** (fast, high-quality, reproducible).

### 5.2 Seed Derivation

The stream-provider must define a stable derivation function that maps:

- `(masterSeed, streamName)` → `streamSeed`

The derivation algorithm must be stable and versioned.

---

## 6. Event-Sourcing Compatibility (sim-world)

Named streams are great for “deterministic from seed”, but event sourcing has an extra constraint:

- If event replay depends on the *exact number/order* of RNG calls, adding a new random call can change downstream outcomes.

Recommended patterns for sim-world:

- **Store outcomes or seeds in events** for any “historically important” randomness.
- Consider a helper utility (future RFC) to derive **per-event seeds** from:
  - world seed
  - canonical tick
  - domain layer
  - entity id
  - event id / causal chain id

This makes replay robust even if internal implementation changes.

---

## 7. Migration Notes

- mung-bean currently contains `MungBean.Determinism` contracts and a PCG implementation.
- After extraction:
  - move these abstractions into `Plate.TimeDete`
  - update mung-bean to reference `Plate.TimeDete.*` packages
  - update sim-world to replace `System.Random` usage with `IRngStreamProvider` (or derived seeds)

---

## 8. Open Questions

- Should we include a shared `DeterministicSeed` helper type and canonical hash/mix utilities in `Plate.TimeDete`?
- Should we standardize a “stream registry” for discoverability (or keep stream names ad-hoc)?
