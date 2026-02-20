using System;
using MessagePack;
using MessagePack.Formatters;

namespace Plate.TimeDete.Time.Primitives;

/// <summary>
/// A MessagePack formatter for <see cref="CanonicalTick"/>.
/// Serializes the tick natively as its underlying 64-bit integer value,
/// ensuring deterministic and compact canonical encoding as required by RFC 0400-002.
/// </summary>
public sealed class CanonicalTickFormatter : IMessagePackFormatter<CanonicalTick>
{
    public static readonly CanonicalTickFormatter Instance = new();

    public void Serialize(ref MessagePackWriter writer, CanonicalTick value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public CanonicalTick Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new CanonicalTick(reader.ReadInt64());
    }
}
