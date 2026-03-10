#nullable enable
using System;
using AdapTypeXR.Core.Models;

namespace AdapTypeXR.Core.Interfaces
{
    /// <summary>
    /// Abstracts the eye tracking hardware so that the domain layer
    /// never depends on a specific SDK (Varjo, OpenXR, mock, etc.).
    /// Follows the Adapter pattern — concrete implementations wrap
    /// the underlying SDK and translate its data into domain types.
    /// </summary>
    public interface IEyeTrackingService
    {
        /// <summary>
        /// Raised every time a new gaze sample is available.
        /// Subscribers should be lightweight; heavy processing should
        /// be deferred to avoid blocking the 200 Hz sample loop.
        /// </summary>
        event Action<GazeDataPoint> GazeSampleReceived;

        /// <summary>
        /// Whether the eye tracker is calibrated and actively providing data.
        /// </summary>
        bool IsTracking { get; }

        /// <summary>
        /// Begins eye tracking. Must be called before samples are emitted.
        /// Idempotent — safe to call if already tracking.
        /// </summary>
        void StartTracking();

        /// <summary>
        /// Stops eye tracking and releases hardware resources.
        /// </summary>
        void StopTracking();

        /// <summary>
        /// Returns the most recent gaze sample synchronously.
        /// Returns null if tracking is not active or data is unavailable.
        /// </summary>
        GazeDataPoint? GetLatestGaze();
    }
}
