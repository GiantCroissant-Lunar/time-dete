# Plate.TimeDete RFCs

This directory contains Request for Comments (RFCs) for the **time-dete** repository.

The goal of **time-dete** is to extract *reusable* building blocks for deterministic simulation and gameplay under the `Plate.TimeDete.*` namespace.

Primary consumers:

- sim-world (world generation + simulation)
- mung-bean (dungeon crawler game)

## RFC Status

| RFC | Title | Status |
|---|---|---|
| [RFC-0001](./RFC-0001-core-architecture.md) | Core Architecture & Scope | Draft |
| [RFC-0002](./RFC-0002-canonical-tick-and-clocks.md) | Canonical Tick & Clock Abstractions | Draft |
| [RFC-0003](./RFC-0003-deterministic-rng-streams.md) | Deterministic RNG & RNG Streams | Draft |
| [RFC-0004](./RFC-0004-hash-chain-traceability.md) | Hash-Chain Traceability (Local Blockchain) | Draft |

## Reading Order

For new contributors:

1. **RFC-0001**: Core Architecture & Scope
2. **RFC-0002**: Canonical Tick & Clock Abstractions
3. **RFC-0003**: Deterministic RNG & RNG Streams
4. **RFC-0004**: Hash-Chain Traceability (Local Blockchain)

## RFC Process

### Statuses

- **Draft**: Initial proposal, open for major changes
- **Review**: Ready for wider review
- **Accepted**: Approved for implementation
- **Implemented**: Fully implemented
- **Superseded**: Replaced by another RFC

### Adding a New RFC

1. Choose the next available RFC number.
2. Create `RFC-XXXX-<slug>.md`.
3. Add it to this `README.md` table.
4. Keep scope tight: prefer multiple small RFCs over one mega-RFC.
