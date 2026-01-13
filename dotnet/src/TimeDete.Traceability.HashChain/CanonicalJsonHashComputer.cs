using System.Text;
using System.Text.Json;
using Plate.TimeDete.Traceability.Hashing;

namespace Plate.TimeDete.Traceability.HashChain;

public static class CanonicalJsonHashComputer
{
    public static string ComputeHash(
        int schemaVersion,
        long tick,
        string eventType,
        string domainLayer,
        string entityType,
        string entityId,
        JsonDocument payload,
        string? previousHash)
    {
        if (eventType is null)
        {
            throw new ArgumentNullException(nameof(eventType));
        }

        if (domainLayer is null)
        {
            throw new ArgumentNullException(nameof(domainLayer));
        }

        if (entityType is null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        if (entityId is null)
        {
            throw new ArgumentNullException(nameof(entityId));
        }

        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        using var canonicalPayloadDoc = JsonDocument.Parse(CanonicalJsonSerializer.Serialize(payload));

        var canonicalObject = new
        {
            schemaVersion,
            tick,
            eventType,
            domainLayer,
            entityType,
            entityId,
            payload = canonicalPayloadDoc.RootElement,
            previousHash = previousHash ?? string.Empty
        };

        var canonicalJson = CanonicalJsonSerializer.Serialize(canonicalObject);
        var bytes = Encoding.UTF8.GetBytes(canonicalJson);
        return Sha256Hex.ComputeLowerHex(bytes);
    }
}
