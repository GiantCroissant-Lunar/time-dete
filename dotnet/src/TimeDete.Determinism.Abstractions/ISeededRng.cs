namespace Plate.TimeDete.Determinism.Abstractions;

/// <summary>
/// Deterministic random number generator interface.
/// Implementations MUST produce identical sequences for identical seeds/states.
/// </summary>
/// <remarks>
/// This is the core abstraction for deterministic gameplay. All game systems
/// that need randomness should use this interface via DI, not System.Random.
/// </remarks>
public interface ISeededRng
{
    /// <summary>
    /// The original seed used to initialize this RNG.
    /// Useful for logging and debugging.
    /// </summary>
    ulong Seed { get; }

    /// <summary>
    /// Generate next unsigned 32-bit integer in the sequence.
    /// This is the core operation; all other methods derive from this.
    /// </summary>
    uint NextUInt32();

    /// <summary>
    /// Generate next unsigned 64-bit integer in the sequence.
    /// </summary>
    ulong NextUInt64();

    /// <summary>
    /// Generate next integer in the range [0, maxExclusive).
    /// </summary>
    /// <param name="maxExclusive">Exclusive upper bound (must be positive)</param>
    int Next(int maxExclusive);

    /// <summary>
    /// Generate next integer in the range [minInclusive, maxExclusive).
    /// </summary>
    /// <param name="minInclusive">Inclusive lower bound</param>
    /// <param name="maxExclusive">Exclusive upper bound</param>
    int Next(int minInclusive, int maxExclusive);

    /// <summary>
    /// Generate next single-precision float in the range [0.0, 1.0).
    /// </summary>
    float NextFloat();

    /// <summary>
    /// Generate next double-precision float in the range [0.0, 1.0).
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Generate next boolean with 50% probability.
    /// </summary>
    bool NextBool();

    /// <summary>
    /// Generate next boolean with specified probability of true.
    /// </summary>
    /// <param name="probability">Probability of returning true [0.0, 1.0]</param>
    bool NextBool(float probability);

    /// <summary>
    /// Create an independent child RNG derived from this RNG's current state.
    /// The child will produce a different sequence than the parent.
    /// Useful for parallel or isolated subsystems.
    /// </summary>
    /// <param name="childSeed">Additional seed to mix into child state</param>
    ISeededRng Fork(ulong childSeed);

    /// <summary>
    /// Get the current internal state for snapshotting.
    /// </summary>
    RngState GetState();

    /// <summary>
    /// Restore internal state from a snapshot.
    /// After calling this, the RNG will produce the same sequence
    /// it would have produced from this state originally.
    /// </summary>
    void SetState(RngState state);

    /// <summary>
    /// Advance the RNG by n steps without generating values.
    /// Useful for synchronization in replay scenarios.
    /// </summary>
    /// <param name="steps">Number of steps to advance</param>
    void Advance(ulong steps);
}
