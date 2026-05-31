using System.Security.Cryptography;
using System.Text;

namespace TimeDete.Traceability.Hashing;

/// <summary>
/// Provides SHA-256 hashing utilities with lowercase hexadecimal output.
/// This is a low-level cryptographic primitive used by hash chains, content addressing,
/// glyph IDs, spec hashes, and other components requiring consistent SHA-256 hashing.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Sha256Hex"/> when you need to <b>compute a SHA-256 hash</b> and get hex output.
/// Use <see cref="HexEncoding"/> when you already have bytes and only need <b>hex encoding/decoding</b>.
/// </para>
/// <para>
/// For hot paths with many hashes, use <see cref="TryComputeLowerHex"/> to avoid all allocations.
/// </para>
/// </remarks>
public static class Sha256Hex
{
    /// <summary>SHA-256 hash output size in bytes.</summary>
    public const int HashSizeBytes = 32;

    /// <summary>SHA-256 hash output size in hex characters.</summary>
    public const int HashSizeHexChars = 64;

    /// <summary>
    /// Computes the SHA-256 hash of the given bytes and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <returns>A 64-character lowercase hexadecimal string.</returns>
    public static string ComputeLowerHex(ReadOnlySpan<byte> bytes)
    {
        using var sha = SHA256.Create();

        Span<byte> hash = stackalloc byte[HashSizeBytes];
        if (!sha.TryComputeHash(bytes, hash, out var written) || written != HashSizeBytes)
        {
            throw new CryptographicException("SHA-256 hash computation failed.");
        }

        return ToLowerHex(hash);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the given byte array and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <returns>A 64-character lowercase hexadecimal string.</returns>
    public static string ComputeLowerHex(byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return ComputeLowerHex(bytes.AsSpan());
    }

    /// <summary>
    /// Computes the SHA-256 hash of the given UTF-8 string and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="utf8">The string to hash (interpreted as UTF-8).</param>
    /// <returns>A 64-character lowercase hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8"/> is null.</exception>
    public static string ComputeLowerHex(string utf8)
    {
        if (utf8 is null)
        {
            throw new ArgumentNullException(nameof(utf8));
        }

        // Get byte count and use stackalloc for small strings
        var byteCount = Encoding.UTF8.GetByteCount(utf8);

        if (byteCount <= 256)
        {
            Span<byte> buffer = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(utf8, buffer);
            return ComputeLowerHex(buffer);
        }

        // Fallback to allocation for large strings
        return ComputeLowerHex(Encoding.UTF8.GetBytes(utf8));
    }

    /// <summary>
    /// Computes the SHA-256 hash and writes the lowercase hex result directly into the destination.
    /// This is the fully allocation-free hot-path API for tight loops.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="destination">The destination span (must be at least 64 chars).</param>
    /// <returns><c>true</c> if successful; <c>false</c> if destination is too small.</returns>
    /// <remarks>
    /// Writes exactly 64 characters to <c>destination[..64]</c>.
    /// Characters beyond index 63 are not modified.
    /// </remarks>
    /// <example>
    /// <code>
    /// Span&lt;char&gt; hexBuffer = stackalloc char[Sha256Hex.HashSizeHexChars];
    /// if (Sha256Hex.TryComputeLowerHex(dataSpan, hexBuffer))
    /// {
    ///     // Use hexBuffer directly, zero allocations
    /// }
    /// </code>
    /// </example>
    public static bool TryComputeLowerHex(ReadOnlySpan<byte> data, Span<char> destination)
    {
        if (destination.Length < HashSizeHexChars)
        {
            return false;
        }

        using var sha = SHA256.Create();
        Span<byte> hash = stackalloc byte[HashSizeBytes];

        if (!sha.TryComputeHash(data, hash, out var written) || written != HashSizeBytes)
        {
            return false;
        }

        WriteLowerHexCore(hash, destination);
        return true;
    }

    /// <summary>
    /// Computes the SHA-256 hash and writes the uppercase hex result directly into the destination.
    /// This is the fully allocation-free hot-path API for tight loops.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="destination">The destination span (must be at least 64 chars).</param>
    /// <returns><c>true</c> if successful; <c>false</c> if destination is too small.</returns>
    /// <remarks>
    /// Writes exactly 64 characters to <c>destination[..64]</c>.
    /// Characters beyond index 63 are not modified.
    /// </remarks>
    public static bool TryComputeUpperHex(ReadOnlySpan<byte> data, Span<char> destination)
    {
        if (destination.Length < HashSizeHexChars)
        {
            return false;
        }

        using var sha = SHA256.Create();
        Span<byte> hash = stackalloc byte[HashSizeBytes];

        if (!sha.TryComputeHash(data, hash, out var written) || written != HashSizeBytes)
        {
            return false;
        }

        WriteUpperHexCore(hash, destination);
        return true;
    }

    /// <summary>
    /// Computes the SHA-256 hash and writes the first <paramref name="hexChars"/> lowercase hex characters.
    /// This supports truncation for deterministic ID generation (e.g., short hashes, glyph IDs).
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="destination">The destination span (must be at least <paramref name="hexChars"/> chars).</param>
    /// <param name="hexChars">Number of hex characters to write (0–64). Truncation takes the prefix.</param>
    /// <returns><c>true</c> if successful; <c>false</c> if destination too small or hexChars out of range.</returns>
    /// <remarks>
    /// Semantics: compute full SHA-256, hex-encode, return the <b>first</b> <paramref name="hexChars"/> characters.
    /// This is the canonical truncation rule across all FantaSim repositories.
    /// </remarks>
    public static bool TryComputeLowerHex(ReadOnlySpan<byte> data, Span<char> destination, int hexChars)
    {
        if (hexChars < 0 || hexChars > HashSizeHexChars || destination.Length < hexChars)
        {
            return false;
        }

        if (hexChars == 0)
        {
            return true;
        }

        using var sha = SHA256.Create();
        Span<byte> hash = stackalloc byte[HashSizeBytes];

        if (!sha.TryComputeHash(data, hash, out var written) || written != HashSizeBytes)
        {
            return false;
        }

        WriteLowerHexTruncated(hash, destination, hexChars);
        return true;
    }

    /// <summary>
    /// Computes the SHA-256 hash and returns the first <paramref name="hexChars"/> lowercase hex characters.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="hexChars">Number of hex characters to return (0–64). Truncation takes the prefix.</param>
    /// <returns>A lowercase hex string of exactly <paramref name="hexChars"/> length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="hexChars"/> is not 0–64.</exception>
    public static string ComputeLowerHex(ReadOnlySpan<byte> data, int hexChars)
    {
        if (hexChars < 0 || hexChars > HashSizeHexChars)
        {
            throw new ArgumentOutOfRangeException(nameof(hexChars), hexChars, "Must be 0–64.");
        }

        if (hexChars == 0)
        {
            return string.Empty;
        }

        Span<char> buffer = stackalloc char[hexChars];
        if (!TryComputeLowerHex(data, buffer, hexChars))
        {
            throw new CryptographicException("SHA-256 hash computation failed.");
        }

        return new string(buffer);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the UTF-8 string and returns the first <paramref name="hexChars"/> lowercase hex characters.
    /// </summary>
    /// <param name="utf8">The string to hash (interpreted as UTF-8).</param>
    /// <param name="hexChars">Number of hex characters to return (0–64). Truncation takes the prefix.</param>
    /// <returns>A lowercase hex string of exactly <paramref name="hexChars"/> length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="hexChars"/> is not 0–64.</exception>
    public static string ComputeLowerHex(string utf8, int hexChars)
    {
        if (utf8 is null)
        {
            throw new ArgumentNullException(nameof(utf8));
        }

        if (hexChars < 0 || hexChars > HashSizeHexChars)
        {
            throw new ArgumentOutOfRangeException(nameof(hexChars), hexChars, "Must be 0–64.");
        }

        if (hexChars == 0)
        {
            return string.Empty;
        }

        var byteCount = Encoding.UTF8.GetByteCount(utf8);

        if (byteCount <= 256)
        {
            Span<byte> buffer = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(utf8, buffer);
            return ComputeLowerHex(buffer, hexChars);
        }

        return ComputeLowerHex(Encoding.UTF8.GetBytes(utf8).AsSpan(), hexChars);
    }

    /// <summary>
    /// Computes the SHA-256 hash and writes the first <paramref name="hexChars"/> uppercase hex characters.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="destination">The destination span (must be at least <paramref name="hexChars"/> chars).</param>
    /// <param name="hexChars">Number of hex characters to write (0–64). Truncation takes the prefix.</param>
    /// <returns><c>true</c> if successful; <c>false</c> if destination too small or hexChars out of range.</returns>
    public static bool TryComputeUpperHex(ReadOnlySpan<byte> data, Span<char> destination, int hexChars)
    {
        if (hexChars < 0 || hexChars > HashSizeHexChars || destination.Length < hexChars)
        {
            return false;
        }

        if (hexChars == 0)
        {
            return true;
        }

        using var sha = SHA256.Create();
        Span<byte> hash = stackalloc byte[HashSizeBytes];

        if (!sha.TryComputeHash(data, hash, out var written) || written != HashSizeBytes)
        {
            return false;
        }

        WriteUpperHexTruncated(hash, destination, hexChars);
        return true;
    }

    /// <summary>
    /// Converts a byte span to a lowercase hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>A lowercase hexadecimal string representation.</returns>
    /// <remarks>
    /// This method exists for convenience. For hex encoding without hashing,
    /// prefer <see cref="HexEncoding.ToLowerHex"/> to make intent clearer.
    /// </remarks>
    public static string ToLowerHex(ReadOnlySpan<byte> bytes)
    {
        // Note: bytes.ToArray() is required because ReadOnlySpan cannot be captured as state
        // For hot paths, use WriteLowerHex with a caller-provided Span<char> instead
        return string.Create(bytes.Length * 2, bytes.ToArray(), static (chars, source) =>
        {
            WriteLowerHexCore(source, chars);
        });
    }

    /// <summary>
    /// Writes the lowercase hexadecimal representation of bytes into the destination span.
    /// This is the allocation-free hot-path API for performance-sensitive code.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="destination">The destination span (must be at least bytes.Length * 2 chars).</param>
    /// <returns>The number of characters written (always bytes.Length * 2).</returns>
    /// <exception cref="ArgumentException">Thrown when destination is too small.</exception>
    public static int WriteLowerHex(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        var requiredLength = bytes.Length * 2;
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException(
                $"Destination too small. Required: {requiredLength}, Available: {destination.Length}",
                nameof(destination));
        }

        WriteLowerHexCore(bytes, destination);
        return requiredLength;
    }

    private static void WriteLowerHexCore(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        const string Hex = "0123456789abcdef";
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            destination[i * 2] = Hex[b >> 4];
            destination[i * 2 + 1] = Hex[b & 0x0F];
        }
    }

    private static void WriteUpperHexCore(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        const string Hex = "0123456789ABCDEF";
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            destination[i * 2] = Hex[b >> 4];
            destination[i * 2 + 1] = Hex[b & 0x0F];
        }
    }

    /// <summary>
    /// Writes exactly <paramref name="hexChars"/> lowercase hex characters from the hash bytes.
    /// Handles odd lengths correctly (writes only the high nibble for the last byte if odd).
    /// </summary>
    private static void WriteLowerHexTruncated(ReadOnlySpan<byte> bytes, Span<char> destination, int hexChars)
    {
        const string Hex = "0123456789abcdef";
        var fullBytes = hexChars / 2;

        for (var i = 0; i < fullBytes; i++)
        {
            var b = bytes[i];
            destination[i * 2] = Hex[b >> 4];
            destination[i * 2 + 1] = Hex[b & 0x0F];
        }

        // If odd hexChars, write just the high nibble of the next byte
        if ((hexChars & 1) == 1)
        {
            var b = bytes[fullBytes];
            destination[hexChars - 1] = Hex[b >> 4];
        }
    }

    /// <summary>
    /// Writes exactly <paramref name="hexChars"/> uppercase hex characters from the hash bytes.
    /// Handles odd lengths correctly (writes only the high nibble for the last byte if odd).
    /// </summary>
    private static void WriteUpperHexTruncated(ReadOnlySpan<byte> bytes, Span<char> destination, int hexChars)
    {
        const string Hex = "0123456789ABCDEF";
        var fullBytes = hexChars / 2;

        for (var i = 0; i < fullBytes; i++)
        {
            var b = bytes[i];
            destination[i * 2] = Hex[b >> 4];
            destination[i * 2 + 1] = Hex[b & 0x0F];
        }

        // If odd hexChars, write just the high nibble of the next byte
        if ((hexChars & 1) == 1)
        {
            var b = bytes[fullBytes];
            destination[hexChars - 1] = Hex[b >> 4];
        }
    }
}
