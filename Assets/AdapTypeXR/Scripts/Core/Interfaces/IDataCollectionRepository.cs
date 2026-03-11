#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using AdapTypeXR.Core.Models;

namespace AdapTypeXR.Core.Interfaces
{
    /// <summary>
    /// Repository contract for persisting reading session data.
    /// Follows the Repository pattern — callers are decoupled from
    /// the underlying storage format (CSV, JSON, database, cloud).
    /// </summary>
    public interface IDataCollectionRepository
    {
        /// <summary>
        /// Persists a single gaze data point. Thread-safe; may be called
        /// from a high-frequency sampling loop.
        /// </summary>
        /// <param name="point">The gaze sample to store.</param>
        Task RecordGazePointAsync(GazeDataPoint point);

        /// <summary>
        /// Persists the summary metrics for a completed reading passage.
        /// </summary>
        /// <param name="metrics">Aggregated reading metrics for the passage.</param>
        Task RecordReadingMetricsAsync(ReadingMetrics metrics);

        /// <summary>
        /// Opens a new session file/context for the given session.
        /// Must be called before any recording methods.
        /// </summary>
        /// <param name="session">Session descriptor containing metadata.</param>
        Task BeginSessionAsync(ReadingSession session);

        /// <summary>
        /// Flushes all buffered data and closes the session context.
        /// </summary>
        Task EndSessionAsync();

        /// <summary>
        /// Persists a participant's response to a comprehension question.
        /// </summary>
        /// <param name="response">The comprehension response to store.</param>
        Task RecordComprehensionResponseAsync(ComprehensionResponse response);

        /// <summary>
        /// Retrieves all sessions available in the data store.
        /// Intended for offline analysis tooling, not real-time use.
        /// </summary>
        Task<IReadOnlyList<ReadingSession>> GetAllSessionsAsync();
    }
}
