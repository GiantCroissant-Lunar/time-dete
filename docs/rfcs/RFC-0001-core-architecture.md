# RFC-0001: Core Architecture & Scope

## Status: Draft

## Created: 2025-12-12

## Depends On: (none)

---

## 1. Overview

This RFC defines the purpose and scope of the **time-dete** repository.

**time-dete** exists to extract reusable infrastructure for:

- **Canonical simulation time** (ticks, clocks)
- **Deterministic random** (seeded RNG + independent RNG streams)

The immediate motivation is to **unify** the time/determinism substrate across:

- **sim-world** (world generation, simulation, event-sourcing)
- **mung-bean** (dungeon crawler game runtime)

---

## 2. Problem Statement

Both sim-world and mung-bean need:

- A shared definition of **simulation time** (tick semantics)
- A shared deterministic RNG story so that:
  - Simulations can be reproduced from a seed/config
  - Replays/debugging can be deterministic
  - The introduction of new random calls in one subsystem does not silently perturb unrelated subsystems

When these are implemented separately per repo, the result is:

- Inconsistent tick/time APIs
- Divergent “determinism” guarantees
- Increased integration friction when sim-world is embedded/consumed by mung-bean

---

## 3. Goals

- Provide a **small**, stable foundation library for tick/time.
- Provide a **small**, stable foundation library for deterministic RNG.
- Be **engine-agnostic**:
  - Works with event-sourced architectures (sim-world)
  - Works with ECS runtimes (mung-bean and future sim-world ECS adoption)
- Be **portable**:
  - No dependency on UI frameworks
  - No dependency on ArangoDB, storage, or host implementations

---

## 4. Non-Goals

- Defining sim-world domain layers, event types, or hash-chain rules.
- Providing a full replay/input-recording framework.
- Providing ECS abstractions (that is handled by unify-ecs).
- Providing a full snapshot system for arbitrary worlds/engines.

---

## 5. Proposed Package Layout

### 5.1 Time

- `Plate.TimeDete.Time.Primitives`
  - `CanonicalTick`
  - (Optional) `TickDelta` / `TickRange` helpers

- `Plate.TimeDete.Time`
  - `ICanonicalClock`

- `Plate.TimeDete.Time.Runtime`
  - `CanonicalClock` (thread-safe, monotonic)

### 5.2 Determinism

- `Plate.TimeDete.Determinism.Abstractions`
  - `ISeededRng`
  - `ISeededRngFactory`
  - `IRngStreamProvider`
  - `RngState`

- `Plate.TimeDete.Determinism.Pcg`
  - PCG-based implementation (optional but recommended)

### 5.3 Traceability

- `Plate.TimeDete.Traceability.HashChain`
  - Hash chain primitives (local blockchain)
  - Deterministic hashing helpers

---

## 6. Compatibility & Targets

- Prefer a target that works for both libraries and games. Candidate targets:
  - `netstandard2.1` (maximum compatibility)
  - or `net8.0` if you want to move fast and don’t need older runtimes

This decision is deferred until the first .NET projects are created.

---

## 7. Determinism Guarantees

- RNG implementations MUST produce identical sequences for identical seeds/state.
- Stream seed derivation MUST be stable and versioned (changing derivation is a breaking change).
- Time primitives MUST be monotonic and non-negative.

---

## 8. Migration Plan (High-Level)

1. Move/copy **CanonicalTick + ICanonicalClock** out of sim-world into time-dete.
2. Move/copy **seeded RNG + stream provider** out of mung-bean into time-dete.
3. Update sim-world and mung-bean to depend on the new packages.
4. Replace remaining `System.Random` usage in sim-world with the deterministic abstractions.

---

## 9. Open Questions

- Root namespace: `Plate.TimeDete.*`.
- Should `CanonicalTick` remain “unitless ordering”, or should we also define a “tick duration” type?
- Do we want a formal compatibility contract for determinism across versions (e.g., algorithm lock + golden tests)?
