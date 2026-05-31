namespace TimeDete.Traceability.Hashing;

/// <summary>
/// Provides hexadecimal encoding and decoding utilities.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="HexEncoding"/> when you already have bytes and only need <b>hex encoding/decoding</b>.
/// Use <see cref="Sha256Hex"/> when you need to <b>compute a SHA-256 hash</b> and get hex output.
/// </para>
/// </remarks>
public static class HexEncoding
{
    private const string LowerHexChars = "0123456789abcdef";
    private const string UpperHexChars = "0123456789ABCDEF";

    /// <summary>
    /// Converts a byte span to a lowercase hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>A lowercase hexadecimal string representation.</returns>
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
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            destination[i * 2] = LowerHexChars[b >> 4];
            destination[i * 2 + 1] = LowerHexChars[b & 0x0F];
        }
    }

    /// <summary>
    /// Converts a byte span to an uppercase hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <returns>An uppercase hexadecimal string representation.</returns>
    public static string ToUpperHex(ReadOnlySpan<byte> bytes)
    {
        // Note: bytes.ToArray() is required because ReadOnlySpan cannot be captured as state
        // For hot paths, use WriteUpperHex with a caller-provided Span<char> instead
        return string.Create(bytes.Length * 2, bytes.ToArray(), static (chars, source) =>
        {
            WriteUpperHexCore(source, chars);
        });
    }

    /// <summary>
    /// Writes the uppercase hexadecimal representation of bytes into the destination span.
    /// This is the allocation-free hot-path API for performance-sensitive code.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="destination">The destination span (must be at least bytes.Length * 2 chars).</param>
    /// <returns>The number of characters written (always bytes.Length * 2).</returns>
    /// <exception cref="ArgumentException">Thrown when destination is too small.</exception>
    public static int WriteUpperHex(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        var requiredLength = bytes.Length * 2;
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException(
                $"Destination too small. Required: {requiredLength}, Available: {destination.Length}",
                nameof(destination));
        }

        WriteUpperHexCore(bytes, destination);
        return requiredLength;
    }

    private static void WriteUpperHexCore(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            destination[i * 2] = UpperHexChars[b >> 4];
            destination[i * 2 + 1] = UpperHexChars[b & 0x0F];
        }
    }

    /// <summary>
    /// Parses a hexadecimal string into a byte array.
    /// </summary>
    /// <param name="hex">The hexadecimal string to parse.</param>
    /// <returns>A byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hex"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="hex"/> has invalid length or characters.</exception>
    public static byte[] FromHex(string hex)
    {
        if (hex is null)
        {
            throw new ArgumentNullException(nameof(hex));
        }

        if (hex.Length % 2 != 0)
        {
            throw new FormatException("Hex string must have an even number of characters.");
        }

        var bytes = new byte[hex.Length / 2];

        for (var i = 0; i < bytes.Length; i++)
        {
            var hi = ParseHexChar(hex[i * 2]);
            var lo = ParseHexChar(hex[i * 2 + 1]);
            bytes[i] = (byte)((hi << 4) | lo);
        }

        return bytes;
    }

    private static int ParseHexChar(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => throw new FormatException($"Invalid hex character: '{c}'"),
        };
    }
}
