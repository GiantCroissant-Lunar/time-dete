namespace TimeDete.Determinism.Abstractions;

/// <summary>
/// Factory for creating seeded RNG instances.
/// </summary>
public interface ISeededRngFactory
{
    /// <summary>
    /// Create a new RNG with the specified seed.
    /// Two RNGs created with the same seed will produce identical sequences.
    /// </summary>
    /// <param name="seed">The seed value</param>
    ISeededRng Create(ulong seed);

    /// <summary>
    /// Create a new RNG from existing state.
    /// Used when restoring from snapshots.
    /// </summary>
    /// <param name="state">The RNG state to restore</param>
    ISeededRng Create(RngState state);

    /// <summary>
    /// Create a new RNG with a random seed.
    /// For new games where reproducibility isn't needed yet.
    /// The seed will be recorded for later replay.
    /// </summary>
    ISeededRng CreateRandom();
}
