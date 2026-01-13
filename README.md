# TimeDete

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-512bd4.svg)](https://dotnet.microsoft.com/)

Reusable infrastructure for **canonical simulation time** and **deterministic randomness** in .NET.

## Overview

**TimeDete** provides a stable foundation for:

- **Canonical Time** - Tick-based simulation time with monotonic clocks
- **Deterministic RNG** - Seeded random number generation with independent streams
- **Traceability** - Hash chain primitives for audit trails and replay verification

Designed to be engine-agnostic and portable, with no dependencies on UI frameworks or storage systems.

## Packages

| Package | Description |
|---------|-------------|
| `TimeDete.Time.Primitives` | Core types: `CanonicalTick`, `TickDelta`, `TickScale` |
| `TimeDete.Time` | Abstractions: `ICanonicalClock`, `EpochMarker` |
| `TimeDete.Time.Runtime` | Runtime implementation: `CanonicalClock` |
| `TimeDete.Determinism.Abstractions` | RNG interfaces: `ISeededRng`, `IRngStreamProvider` |
| `TimeDete.Determinism.Pcg` | PCG-based deterministic RNG implementation |
| `TimeDete.Traceability.Hashing` | SHA-256 hex encoding utilities |
| `TimeDete.Traceability.HashChain` | Hash chain primitives for traceability |

## Quick Start

### Canonical Time

```csharp
using TimeDete.Time.Primitives;
using TimeDete.Time.Runtime;

// Create a canonical clock
var clock = new CanonicalClock();

// Get current tick
CanonicalTick currentTick = clock.CurrentTick;

// Advance time
clock.Advance(TickDelta.FromTicks(10));
```

### Deterministic RNG

```csharp
using TimeDete.Determinism.Pcg;

// Create a seeded RNG factory
var factory = new PcgSeededRngFactory();

// Create RNG with specific seed
var rng = factory.Create(seed: 12345UL);

// Generate deterministic random values
int value = rng.NextInt(0, 100);
double normalized = rng.NextDouble();
```

### RNG Streams

```csharp
using TimeDete.Determinism.Pcg;

// Create stream provider for independent RNG streams
var provider = new PcgRngStreamProvider(masterSeed: 42UL);

// Get independent streams by name (deterministic derivation)
var combatRng = provider.GetStream("combat");
var lootRng = provider.GetStream("loot");

// Each stream is independent - changes to one don't affect others
```

## Determinism Guarantees

- RNG implementations produce **identical sequences** for identical seeds
- Stream seed derivation is **stable and versioned**
- Time primitives are **monotonic and non-negative**

## Building

```bash
cd dotnet
dotnet build
dotnet test
```

## Documentation

See the [docs/rfcs](docs/rfcs) directory for design documents:

- [RFC-0001: Core Architecture](docs/rfcs/RFC-0001-core-architecture.md)
- [RFC-0002: Canonical Tick and Clocks](docs/rfcs/RFC-0002-canonical-tick-and-clocks.md)
- [RFC-0003: Deterministic RNG Streams](docs/rfcs/RFC-0003-deterministic-rng-streams.md)
- [RFC-0004: Hash Chain Traceability](docs/rfcs/RFC-0004-hash-chain-traceability.md)

## License

This project is licensed under the MIT License.
