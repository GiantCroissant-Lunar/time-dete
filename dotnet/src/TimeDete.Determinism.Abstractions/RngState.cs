using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace TimeDete.Determinism.Abstractions;

/// <summary>
/// Serializable RNG state for snapshots and replay.
/// Uses 128-bit state (compatible with PCG64 and similar algorithms).
/// </summary>
/// <param name="State">The internal state of the RNG</param>
/// <param name="Inc">The increment/stream selector (for PCG variants)</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RngState(ulong State, ulong Inc)
{
    /// <summary>
    /// Create state from a single seed (derives both state and inc).
    /// </summary>
    public static RngState FromSeed(ulong seed)
    {
        // Use different bits for state and inc to ensure good distribution
        return new RngState(
            State: seed ^ 0x5A17_A73D_392F_1B89UL,
            Inc: (seed >> 1) | 1  // Inc must be odd for PCG
        );
    }

    /// <summary>
    /// Serialize to byte array (16 bytes).
    /// </summary>
    public byte[] ToBytes()
    {
        var bytes = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(0, 8), State);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(8, 8), Inc);
        return bytes;
    }

    /// <summary>
    /// Deserialize from byte array.
    /// </summary>
    public static RngState FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 16)
            throw new ArgumentException("RngState requires 16 bytes", nameof(bytes));

        return new RngState(
            State: BinaryPrimitives.ReadUInt64LittleEndian(bytes[..8]),
            Inc: BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..16])
        );
    }
}
