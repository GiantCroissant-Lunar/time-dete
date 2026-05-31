namespace TimeDete.Determinism.Abstractions;

/// <summary>
/// Provides named RNG streams for different game systems.
/// Each stream is deterministic and independent, allowing
/// different systems to consume random numbers without
/// affecting each other's sequences.
/// </summary>
/// <remarks>
/// This is the recommended way for game systems to access RNG.
/// Each system gets its own named stream, ensuring that adding
/// randomness to one system doesn't change the behavior of others.
/// </remarks>
/// <example>
/// <code>
/// public class LootSystem
/// {
///     private readonly ISeededRng _rng;
///     
///     public LootSystem(IRngStreamProvider provider)
///     {
///         _rng = provider.GetStream("game.loot");
///     }
///     
///     public Item GenerateLoot()
///     {
///         return _rng.Next(100) &lt; 10 ? RareItem : CommonItem;
///     }
/// }
/// </code>
/// </example>
public interface IRngStreamProvider
{
    /// <summary>
    /// Get or create a named RNG stream.
    /// If the stream already exists, returns the existing instance.
    /// If not, creates a new stream derived from the master seed.
    /// </summary>
    /// <param name="name">Stream name (e.g., "game.loot", "game.combat")</param>
    ISeededRng GetStream(string name);

    /// <summary>
    /// Check if a named stream exists.
    /// </summary>
    bool HasStream(string name);

    /// <summary>
    /// Get all stream names currently registered.
    /// </summary>
    IReadOnlyCollection<string> GetStreamNames();

    /// <summary>
    /// Capture the state of all streams for snapshotting.
    /// </summary>
    IReadOnlyDictionary<string, RngState> CaptureState();

    /// <summary>
    /// Restore all stream states from a snapshot.
    /// Streams not in the snapshot are removed.
    /// Streams in the snapshot that don't exist are created.
    /// </summary>
    void RestoreState(IReadOnlyDictionary<string, RngState> states);

    /// <summary>
    /// Reset all streams and reinitialize from master seed.
    /// </summary>
    /// <param name="masterSeed">New master seed for all streams</param>
    void Reset(ulong masterSeed);

    /// <summary>
    /// The master seed used to derive stream seeds.
    /// </summary>
    ulong MasterSeed { get; }
}
