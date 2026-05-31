using System.Text.Json;
using TimeDete.Traceability.HashChain;
using TimeDete.Traceability.Hashing;

namespace TimeDete.Traceability.HashChain.Tests;

public sealed class HashChainTests
{
    private const string CanonicalJsonCase1 =
        "{\"domainLayer\":\"World\",\"entityId\":\"world-1\",\"entityType\":\"World\",\"eventType\":\"TestEvent\",\"payload\":{\"a\":1,\"b\":2},\"previousHash\":\"\",\"schemaVersion\":1,\"tick\":123}";

    // Canonical envelope with previousHash: null
    private const string CanonicalEnvelopeCase1NullPrevious =
        "{\"branchId\":\"main\",\"canonicalTick\":123,\"chainKey\":\"world_001:main:town\",\"payload\":{\"a\":1,\"b\":2},\"previousHash\":null,\"recordType\":\"TownEvent\",\"schemaVersion\":1,\"worldId\":\"world_001\"}";

    // Canonical envelope with previousHash: "prev"
    private const string CanonicalEnvelopeCase1WithPrevious =
        "{\"branchId\":\"main\",\"canonicalTick\":123,\"chainKey\":\"world_001:main:town\",\"payload\":{\"a\":1,\"b\":2},\"previousHash\":\"prev\",\"recordType\":\"TownEvent\",\"schemaVersion\":1,\"worldId\":\"world_001\"}";

    [Fact]
    public void CanonicalJsonHashComputer_Case1_HashMatchesSha256OfCanonicalJson()
    {
        using var payload = JsonDocument.Parse("{\"b\":2,\"a\":1}");

        var expectedHash = Sha256Hex.ComputeLowerHex(CanonicalJsonCase1);

        var actualHash = CanonicalJsonHashComputer.ComputeHash(
            schemaVersion: 1,
            tick: 123,
            eventType: "TestEvent",
            domainLayer: "World",
            entityType: "World",
            entityId: "world-1",
            payload: payload,
            previousHash: null);

        Assert.Equal(expectedHash, actualHash);
    }

    private sealed record TestChainEvent(
        string Id,
        long Tick,
        DateTimeOffset RecordedAt,
        string PayloadJson,
        string? PreviousHash,
        string Hash);

    private static List<TestChainEvent> CreateValidChain(int count)
    {
        var events = new List<TestChainEvent>(count);
        string? previousHash = null;

        for (var i = 0; i < count; i++)
        {
            var payloadJson = "{\"index\":" + i + "}";

            string ComputeHash(string? prev)
            {
                using var payload = JsonDocument.Parse(payloadJson);
                return CanonicalJsonHashComputer.ComputeHash(
                    schemaVersion: 1,
                    tick: i,
                    eventType: "TestEvent",
                    domainLayer: "World",
                    entityType: "World",
                    entityId: "world_001",
                    payload: payload,
                    previousHash: prev);
            }

            var hash = ComputeHash(previousHash);

            events.Add(new TestChainEvent(
                Id: i.ToString("D4"),
                Tick: i,
                RecordedAt: DateTimeOffset.UnixEpoch.AddSeconds(i),
                PayloadJson: payloadJson,
                PreviousHash: previousHash,
                Hash: hash));

            previousHash = hash;
        }

        return events;
    }

    [Fact]
    public void HashChainVerifier_ValidChain_ReturnsValidResult()
    {
        var events = CreateValidChain(10);

        var result = HashChainVerifier.Verify(
            events,
            chainKey: new ChainKey("world_001:World"),
            getTick: e => e.Tick,
            getRecordedAt: e => e.RecordedAt,
            getPreviousHash: e => e.PreviousHash,
            getHash: e => e.Hash,
            computeHash: (e, prev) =>
            {
                using var payload = JsonDocument.Parse(e.PayloadJson);
                return CanonicalJsonHashComputer.ComputeHash(
                    schemaVersion: 1,
                    tick: e.Tick,
                    eventType: "TestEvent",
                    domainLayer: "World",
                    entityType: "World",
                    entityId: "world_001",
                    payload: payload,
                    previousHash: prev);
            },
            getId: e => e.Id);

        Assert.True(result.IsValid);
        Assert.Equal(10, result.EventsChecked);
        Assert.Null(result.FirstCorruptedEventId);
    }

    [Fact]
    public void HashChainVerifier_TamperedHash_IsDetected()
    {
        var events = CreateValidChain(5);
        var tampered = events[2] with { Hash = new string('0', 64) };
        events[2] = tampered;

        var result = HashChainVerifier.Verify(
            events,
            chainKey: new ChainKey("world_001:World"),
            getTick: e => e.Tick,
            getRecordedAt: e => e.RecordedAt,
            getPreviousHash: e => e.PreviousHash,
            getHash: e => e.Hash,
            computeHash: (e, prev) =>
            {
                using var payload = JsonDocument.Parse(e.PayloadJson);
                return CanonicalJsonHashComputer.ComputeHash(
                    schemaVersion: 1,
                    tick: e.Tick,
                    eventType: "TestEvent",
                    domainLayer: "World",
                    entityType: "World",
                    entityId: "world_001",
                    payload: payload,
                    previousHash: prev);
            },
            getId: e => e.Id);

        Assert.False(result.IsValid);
        Assert.Equal(tampered.Id, result.FirstCorruptedEventId);
    }

    [Fact]
    public void HashChainVerifier_BrokenLink_IsDetected()
    {
        var events = CreateValidChain(5);
        var broken = events[3] with { PreviousHash = "invalid_previous_hash" };
        events[3] = broken;

        var result = HashChainVerifier.Verify(
            events,
            chainKey: new ChainKey("world_001:World"),
            getTick: e => e.Tick,
            getRecordedAt: e => e.RecordedAt,
            getPreviousHash: e => e.PreviousHash,
            getHash: e => e.Hash,
            computeHash: (e, prev) =>
            {
                using var payload = JsonDocument.Parse(e.PayloadJson);
                return CanonicalJsonHashComputer.ComputeHash(
                    schemaVersion: 1,
                    tick: e.Tick,
                    eventType: "TestEvent",
                    domainLayer: "World",
                    entityType: "World",
                    entityId: "world_001",
                    payload: payload,
                    previousHash: prev);
            },
            getId: e => e.Id);

        Assert.False(result.IsValid);
        Assert.Equal(broken.Id, result.FirstCorruptedEventId);
    }

    [Fact]
    public void Sha256HashChainHasher_NullPreviousHash_EqualsSha256OfPayload()
    {
        var hasher = new Sha256HashChainHasher();
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes("payload");

        var expected = Sha256Hex.ComputeLowerHex(payloadBytes);
        var actual = hasher.ComputeHash(payloadBytes, previousHash: null);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Sha256HashChainHasher_WithPreviousHash_EqualsSha256OfPreviousPlusPayload()
    {
        var hasher = new Sha256HashChainHasher();
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes("payload");

        var expected = Sha256Hex.ComputeLowerHex("prevpayload");
        var actual = hasher.ComputeHash(payloadBytes, previousHash: "prev");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HashChain_VerifyLink_ReturnsTrueForMatchingHash()
    {
        var hasher = new Sha256HashChainHasher();
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes("payload");
        var expectedHash = HashChain.ComputeNextHash(hasher, payloadBytes, previousHash: "prev");

        var ok = HashChain.VerifyLink(hasher, payloadBytes, previousHash: "prev", expectedHash: expectedHash);

        Assert.True(ok);
    }

    [Fact]
    public void CanonicalEnvelopeHashComputer_Case1_HashMatchesSha256OfPreviousPlusCanonicalEnvelopeJson()
    {
        var envelope = new HashChainRecordEnvelope<object>(
            ChainKey: new ChainKey("world_001:main:town"),
            BranchId: new BranchId("main"),
            WorldId: "world_001",
            CanonicalTick: 123,
            RecordType: "TownEvent",
            Payload: new { b = 2, a = 1 },
            PreviousHash: null);

        // The canonical JSON includes previousHash:null, then Sha256HashChainHasher prepends empty string for null previousHash
        var expectedHash = Sha256Hex.ComputeLowerHex(CanonicalEnvelopeCase1NullPrevious);

        var actualHash = CanonicalEnvelopeHashComputer.ComputeHash(
            schemaVersion: 1,
            envelope: envelope,
            previousHash: null);

        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public void CanonicalEnvelopeHashComputer_WithPreviousHash_HashMatchesSha256OfPreviousPlusCanonicalEnvelopeJson()
    {
        var envelope = new HashChainRecordEnvelope<object>(
            ChainKey: new ChainKey("world_001:main:town"),
            BranchId: new BranchId("main"),
            WorldId: "world_001",
            CanonicalTick: 123,
            RecordType: "TownEvent",
            Payload: new { b = 2, a = 1 },
            PreviousHash: null);

        // The canonical JSON includes previousHash:"prev", then Sha256HashChainHasher prepends "prev" again
        // This double-chaining is intentional: previousHash is part of the canonical content AND the chain mechanism
        var expectedHash = Sha256Hex.ComputeLowerHex("prev" + CanonicalEnvelopeCase1WithPrevious);

        var actualHash = CanonicalEnvelopeHashComputer.ComputeHash(
            schemaVersion: 1,
            envelope: envelope,
            previousHash: "prev");

        Assert.Equal(expectedHash, actualHash);
    }
}

/// <summary>
/// Tests for Sha256Hex truncation overloads (used by DeterministicHash-style scenarios).
/// </summary>
public sealed class Sha256HexTruncationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(63)]
    [InlineData(64)]
    public void ComputeLowerHex_Truncated_ReturnsPrefixOfFullHash(int hexChars)
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test input");
        var fullHash = Sha256Hex.ComputeLowerHex(data);
        var truncated = Sha256Hex.ComputeLowerHex(data, hexChars);

        Assert.Equal(hexChars, truncated.Length);
        Assert.Equal(fullHash[..hexChars], truncated);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65)]
    [InlineData(100)]
    public void ComputeLowerHex_Truncated_ThrowsForInvalidHexChars(int hexChars)
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test");
        Assert.Throws<ArgumentOutOfRangeException>(() => Sha256Hex.ComputeLowerHex(data, hexChars));
    }

    [Fact]
    public void ComputeLowerHex_StringTruncated_ReturnsPrefixOfFullHash()
    {
        const string input = "hello world";
        var fullHash = Sha256Hex.ComputeLowerHex(input);
        var truncated = Sha256Hex.ComputeLowerHex(input, 16);

        Assert.Equal(16, truncated.Length);
        Assert.Equal(fullHash[..16], truncated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(64)]
    public void TryComputeLowerHex_Truncated_WritesPrefixToDestination(int hexChars)
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test data");
        var fullHash = Sha256Hex.ComputeLowerHex(data);

        Span<char> buffer = stackalloc char[hexChars];
        var result = Sha256Hex.TryComputeLowerHex(data, buffer, hexChars);

        Assert.True(result);
        Assert.Equal(fullHash[..hexChars], new string(buffer));
    }

    [Fact]
    public void TryComputeLowerHex_Truncated_ReturnsFalseIfDestinationTooSmall()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test");
        Span<char> buffer = stackalloc char[7]; // Requesting 8 chars but only 7 available

        var result = Sha256Hex.TryComputeLowerHex(data, buffer, 8);

        Assert.False(result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65)]
    public void TryComputeLowerHex_Truncated_ReturnsFalseForInvalidHexChars(int hexChars)
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test");
        Span<char> buffer = stackalloc char[100];

        var result = Sha256Hex.TryComputeLowerHex(data, buffer, hexChars);

        Assert.False(result);
    }

    [Fact]
    public void TryComputeUpperHex_Truncated_WritesUppercasePrefixToDestination()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test");
        var fullLower = Sha256Hex.ComputeLowerHex(data);

        Span<char> buffer = stackalloc char[16];
        var result = Sha256Hex.TryComputeUpperHex(data, buffer, 16);

        Assert.True(result);
        Assert.Equal(fullLower[..16].ToUpperInvariant(), new string(buffer));
    }

    [Fact]
    public void ComputeLowerHex_Truncated_MatchesDeterministicHashSemantics()
    {
        // This test verifies that the truncation semantics match what DeterministicHash.cs does:
        // 1. Compute full SHA-256
        // 2. Hex-encode
        // 3. Take first N characters

        const string input = "deterministic input";
        const int hexChars = 12;

        // Our new API
        var truncated = Sha256Hex.ComputeLowerHex(input, hexChars);

        // Simulating the old DeterministicHash approach
        var fullHash = Sha256Hex.ComputeLowerHex(input);
        var expected = fullHash[..hexChars];

        Assert.Equal(expected, truncated);
    }
}
