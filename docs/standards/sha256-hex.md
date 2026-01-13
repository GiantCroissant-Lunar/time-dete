# SHA-256 Hex Standards

**Status**: Normative  
**Applies to**: All FantaSim repositories  
**Last updated**: 2026-01-13

## Canonical Package

`TimeDete.Traceability.Hashing` is the **lowest-level** dependency for all SHA-256 → hex conversions.
It is safe for contracts and shared libraries.

```
TimeDete.Traceability.Hashing  (leaf, System.* only, multi-targeted)
        ↑
TimeDete.Traceability.HashChain (protocol layer, JSON canonicalization)
```

## Approved APIs

| Need | Use |
|------|-----|
| Hash bytes → 64-char lowercase hex | `Sha256Hex.ComputeLowerHex(bytes)` |
| Hash UTF-8 string → hex | `Sha256Hex.ComputeLowerHex(string)` |
| **Truncated hash** (N chars) | `Sha256Hex.ComputeLowerHex(data, hexChars)` |
| Hot-path (zero alloc) | `Sha256Hex.TryComputeLowerHex(data, dest)` |
| Hot-path + truncation | `Sha256Hex.TryComputeLowerHex(data, dest, hexChars)` |
| Bytes → hex (no hashing) | `HexEncoding.ToLowerHex(bytes)` |
| Hex → bytes | `HexEncoding.FromHex(hex)` |
| Chain semantics | `TimeDete.Traceability.HashChain` |

## Canonical Rules

### Casing

**Lowercase hex is canonical** for:
- Content-addressed IDs
- Spec hashes  
- Event chain hashes
- Storage keys
- Logs

Uppercase hex only for UI/presentation when explicitly required.

### Truncation

**Truncation = prefix of full SHA-256 hex**

When generating short IDs (e.g., 8-char, 16-char deterministic hashes):

1. Compute the full SHA-256 hash (32 bytes)
2. Hex-encode to lowercase (64 chars)
3. Return the **first N characters** (prefix)

```csharp
// Correct: 16-char truncated hash
var shortId = Sha256Hex.ComputeLowerHex(data, 16);

// Zero-allocation variant
Span<char> buffer = stackalloc char[16];
Sha256Hex.TryComputeLowerHex(data, buffer, 16);
```

**Never**:
- Hash fewer bytes then hex-encode
- Hash then truncate bytes before hex-encoding
- Use substring on a different encoding

### UTF-8 Encoding

**`Encoding.UTF8` without BOM** is the canonical text encoding.

All `ComputeLowerHex(string)` overloads use `Encoding.UTF8.GetBytes()` internally.

## Migration Checklist

### Replace these patterns

| Pattern | Replace with |
|---------|--------------|
| `SHA256.Create()` + manual hex | `Sha256Hex.ComputeLowerHex(...)` |
| `SHA256.HashData(...)` + hex | `Sha256Hex.ComputeLowerHex(...)` |
| `BitConverter.ToString(...).Replace("-", "")` | `HexEncoding.ToLowerHex(...)` |
| `Convert.ToHexString(...)` | `HexEncoding.ToLowerHex(...)` or `ToUpperHex` |
| Custom `ToHex(...)` helpers | `HexEncoding.ToLowerHex(...)` |
| Custom `FromHex(...)` helpers | `HexEncoding.FromHex(...)` |

### Leave alone

| Code | Reason |
|------|--------|
| HashChain protocol code | Keep in `TimeDete.Traceability.HashChain` |
| `HMACSHA256` / signatures | Different crypto semantics |
| Hashing to bytes (not hex) | Use `SHA256` directly |
| Base64 output | Not hex |

## Finding Candidates

```powershell
# Find duplicate SHA-256/hex helpers across fantasim-*
Get-ChildItem -Recurse -Include *.cs |
  Select-String -Pattern "SHA256\.Create|SHA256\.HashData|ComputeHash|BitConverter\.ToString|Convert\.ToHexString|ToHex|ToLowerHex|FromHex" |
  Where-Object { $_.Path -notmatch "TimeDete\.Traceability" }
```

## Hot-Path Example

```csharp
// Zero allocations (full hash)
Span<char> hex = stackalloc char[Sha256Hex.HashSizeHexChars];
if (Sha256Hex.TryComputeLowerHex(dataSpan, hex))
{
    // Use hex[..64] directly
}

// Zero allocations (truncated)
Span<char> shortHex = stackalloc char[16];
if (Sha256Hex.TryComputeLowerHex(dataSpan, shortHex, 16))
{
    // Use shortHex[..16] directly
}
```

## Constants

```csharp
Sha256Hex.HashSizeBytes    // 32
Sha256Hex.HashSizeHexChars // 64
```

## PR Checklist for Migration

When refactoring SHA-256/hex code to use the canonical implementation:

- [ ] Removed local `SHA256→hex` helpers; replaced with `TimeDete.Traceability.Hashing`
- [ ] Kept domain wrapper types (e.g., `Sha256Hash` struct) intact
- [ ] Verified casing matches canonical (lowercase unless UI requires uppercase)
- [ ] Verified truncation semantics: prefix of full hex, not bytes
- [ ] No new dependency arrows upward (Hashing remains leaf)
- [ ] Tests pass with identical output

## Ecosystem Migration Status

| File | Repo | Status | Notes |
|------|------|--------|-------|
| `Sha256Hash.cs` | fantasim-shared | ✅ Done | Internals replaced, wrapper kept |
| `CheckpointHash.cs` | fantasim-world | ✅ Done | Direct replacement |
| `JsonSnapshotSelector.cs` | fantasim-world | ✅ Done | Added Hashing using |
| `DeterministicHash.cs` | fantasim-world | ✅ Done | Delegate to `Sha256Hex.ComputeLowerHex(string, hexChars)` |
| `DomainEventIdComputer.cs` | fantasim-shared | ✅ Done | Already uses `Sha256Hex` |
| `DomainEventPayloadCanonicalizer.cs` | fantasim-shared | ✅ Done | Already uses `Sha256Hex` |
