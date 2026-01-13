# RFC-0002: Canonical Tick & Clock Abstractions

## Status: Draft

## Created: 2025-12-12

## Depends On: RFC-0001

---

## 1. Overview

This RFC defines the **canonical simulation time** substrate for time-dete.

The central concept is **CanonicalTick**:

- Absolute, monotonic, non-negative tick counter
- Suitable as the “time coordinate” for deterministic simulation
- Compatible with both:
  - event-sourced systems (tick stamped events)
  - ECS/system loops (tick-driven update)

---

## 2. Design Principles

- **Ticks are ordering, not wall-clock time**.
- **All simulation time is expressed as ticks**, not `DateTime`.
- **The library never assumes what a tick “means”** (seconds, years, turns, etc.).

---

## 3. CanonicalTick

### 3.1 Data Model

Proposed API shape (illustrative):

```csharp
public readonly record struct CanonicalTick : IComparable<CanonicalTick>
{
    public long Value { get; }

    public CanonicalTick(long value);

    public static readonly CanonicalTick Genesis;

    public CanonicalTick Next();
    public CanonicalTick Add(long delta);
    public CanonicalTick Subtract(long delta);

    public int CompareTo(CanonicalTick other);
}
```

### 3.2 Invariants

- `Value >= 0`
- `Add(delta)` requires `delta >= 0`
- `Subtract(delta)` requires `delta >= 0` and cannot produce negative

### 3.3 Serialization

- Wire/storage format is `long`.
- When used in JSON payloads, the field name should generally be `tick`.

---

## 4. Clock Abstractions

### 4.1 ICanonicalClock

The clock is the authoritative source of “current tick” for a simulation.

```csharp
public interface ICanonicalClock
{
    CanonicalTick CurrentTick { get; }

    void Advance(long ticks = 1);
}
```

### 4.2 CanonicalClock (Runtime Implementation)

A default implementation should:

- Be thread-safe
- Guarantee monotonic progression
- Not allow rewinding

---

## 5. ECS Integration Notes

- An ECS “world step” should generally:
  - read `clock.CurrentTick`
  - run systems for that tick
  - call `clock.Advance(1)` exactly once at the end of the step

This keeps tick progression consistent regardless of system scheduling.

---

## 6. Migration Notes

- sim-world currently defines `CanonicalTick` and `ICanonicalClock` under `SimWorld.Time.*`.
- After extraction, sim-world should reference `Plate.TimeDete.Time.*` and remove local duplicates.

---

## 7. Open Questions

- Do we need a first-class “tick delta” type (e.g., `TickDelta`) to avoid passing raw `long`?
- Should we include `Epoch` concepts here, or leave those to higher-level simulation code?
