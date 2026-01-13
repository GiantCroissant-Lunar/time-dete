using System.Text;

namespace Plate.TimeDete.Traceability.HashChain;

public static class CanonicalEnvelopeHashComputer
{
    public static string ComputeHash<TPayload>(
        int schemaVersion,
        HashChainRecordEnvelope<TPayload> envelope,
        string? previousHash)
    {
        if (envelope is null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        if (envelope.RecordType is null)
        {
            throw new ArgumentNullException(nameof(envelope.RecordType));
        }

        if (envelope.WorldId is null)
        {
            throw new ArgumentNullException(nameof(envelope.WorldId));
        }

        var canonicalObject = new
        {
            schemaVersion,
            chainKey = envelope.ChainKey.Value,
            branchId = envelope.BranchId.Value,
            worldId = envelope.WorldId,
            canonicalTick = envelope.CanonicalTick,
            recordType = envelope.RecordType,
            payload = envelope.Payload,
            previousHash
        };

        var canonicalJson = CanonicalJsonSerializer.Serialize(canonicalObject);
        var payloadBytes = Encoding.UTF8.GetBytes(canonicalJson);

        return new Sha256HashChainHasher().ComputeHash(payloadBytes, previousHash);
    }
}
