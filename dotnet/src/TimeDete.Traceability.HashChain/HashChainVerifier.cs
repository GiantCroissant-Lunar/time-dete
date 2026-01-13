namespace Plate.TimeDete.Traceability.HashChain;

public static class HashChainVerifier
{
    public static HashChainVerificationResult Verify<T>(
        IReadOnlyList<T> events,
        ChainKey chainKey,
        Func<T, long> getTick,
        Func<T, DateTimeOffset> getRecordedAt,
        Func<T, string?> getPreviousHash,
        Func<T, string> getHash,
        Func<T, string?, string> computeHash,
        Func<T, string> getId)
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        if (string.IsNullOrEmpty(chainKey.Value))
        {
            throw new ArgumentException("ChainKey.Value must be non-empty.", nameof(chainKey));
        }

        if (getTick is null)
        {
            throw new ArgumentNullException(nameof(getTick));
        }

        if (getRecordedAt is null)
        {
            throw new ArgumentNullException(nameof(getRecordedAt));
        }

        if (getPreviousHash is null)
        {
            throw new ArgumentNullException(nameof(getPreviousHash));
        }

        if (getHash is null)
        {
            throw new ArgumentNullException(nameof(getHash));
        }

        if (computeHash is null)
        {
            throw new ArgumentNullException(nameof(computeHash));
        }

        if (getId is null)
        {
            throw new ArgumentNullException(nameof(getId));
        }

        if (events.Count == 0)
        {
            return new HashChainVerificationResult
            {
                ChainKey = chainKey,
                IsValid = true,
                EventsChecked = 0,
                FirstCorruptedEventId = null,
                FirstCorruptedEventIndex = null,
                Message = "No events to verify."
            };
        }

        var ordered = events
            .OrderBy(getTick)
            .ThenBy(getRecordedAt)
            .ToList();

        string? expectedPreviousHash = null;
        long checkedCount = 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            var evt = ordered[i];
            checkedCount++;

            if (!string.Equals(getPreviousHash(evt), expectedPreviousHash, StringComparison.Ordinal))
            {
                return new HashChainVerificationResult
                {
                    ChainKey = chainKey,
                    IsValid = false,
                    EventsChecked = checkedCount,
                    FirstCorruptedEventId = getId(evt),
                    FirstCorruptedEventIndex = i,
                    Message = "Previous hash mismatch detected in event sequence."
                };
            }

            var computedHash = computeHash(evt, expectedPreviousHash);
            if (!string.Equals(getHash(evt), computedHash, StringComparison.OrdinalIgnoreCase))
            {
                return new HashChainVerificationResult
                {
                    ChainKey = chainKey,
                    IsValid = false,
                    EventsChecked = checkedCount,
                    FirstCorruptedEventId = getId(evt),
                    FirstCorruptedEventIndex = i,
                    Message = "Stored hash does not match recomputed hash."
                };
            }

            expectedPreviousHash = getHash(evt);
        }

        return new HashChainVerificationResult
        {
            ChainKey = chainKey,
            IsValid = true,
            EventsChecked = checkedCount,
            FirstCorruptedEventId = null,
            FirstCorruptedEventIndex = null,
            Message = "Hash chain verified successfully."
        };
    }
}
