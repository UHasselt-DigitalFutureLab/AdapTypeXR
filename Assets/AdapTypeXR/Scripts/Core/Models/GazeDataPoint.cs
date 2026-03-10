using System;
using UnityEngine;

namespace AdapTypeXR.Core.Models
{
    /// <summary>
    /// Immutable record of a single gaze sample captured by the eye tracker.
    /// All world-space coordinates use Unity's left-handed coordinate system.
    /// Timestamp is in UTC to support cross-device session merging.
    /// </summary>
    public sealed class GazeDataPoint
    {
        /// <summary>UTC timestamp of the sample.</summary>
        public DateTime Timestamp { get; }

        /// <summary>World-space origin of the combined gaze ray.</summary>
        public Vector3 GazeOrigin { get; }

        /// <summary>Normalised world-space direction of the combined gaze ray.</summary>
        public Vector3 GazeDirection { get; }

        /// <summary>World-space point where the gaze ray intersected scene geometry, if any.</summary>
        public Vector3? HitPoint { get; }

        /// <summary>Identifier of the GameObject the gaze ray hit, if any.</summary>
        public string? HitObjectId { get; }

        /// <summary>
        /// Left pupil diameter in millimetres.
        /// NaN if the eye is not tracked or status is invalid.
        /// </summary>
        public float LeftPupilDiameterMm { get; }

        /// <summary>
        /// Right pupil diameter in millimetres.
        /// NaN if the eye is not tracked or status is invalid.
        /// </summary>
        public float RightPupilDiameterMm { get; }

        /// <summary>
        /// Openness of the left eye in [0, 1], where 0 is fully closed (blink).
        /// </summary>
        public float LeftEyeOpenness { get; }

        /// <summary>
        /// Openness of the right eye in [0, 1], where 0 is fully closed (blink).
        /// </summary>
        public float RightEyeOpenness { get; }

        /// <summary>
        /// ID of the reading session this sample belongs to.
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        /// ID of the typography condition active when this sample was captured.
        /// </summary>
        public string ConditionId { get; }

        public GazeDataPoint(
            DateTime timestamp,
            Vector3 gazeOrigin,
            Vector3 gazeDirection,
            Vector3? hitPoint,
            string? hitObjectId,
            float leftPupilDiameterMm,
            float rightPupilDiameterMm,
            float leftEyeOpenness,
            float rightEyeOpenness,
            string sessionId,
            string conditionId)
        {
            Timestamp = timestamp;
            GazeOrigin = gazeOrigin;
            GazeDirection = gazeDirection;
            HitPoint = hitPoint;
            HitObjectId = hitObjectId;
            LeftPupilDiameterMm = leftPupilDiameterMm;
            RightPupilDiameterMm = rightPupilDiameterMm;
            LeftEyeOpenness = leftEyeOpenness;
            RightEyeOpenness = rightEyeOpenness;
            SessionId = sessionId;
            ConditionId = conditionId;
        }

        /// <summary>
        /// Returns true if either eye is fully closed (blink event).
        /// Uses a 0.1 threshold to tolerate sensor noise.
        /// </summary>
        public bool IsBlink => LeftEyeOpenness < 0.1f || RightEyeOpenness < 0.1f;

        /// <summary>
        /// Mean pupil diameter across both eyes. Returns NaN if both are invalid.
        /// </summary>
        public float MeanPupilDiameterMm
        {
            get
            {
                bool leftValid = !float.IsNaN(LeftPupilDiameterMm);
                bool rightValid = !float.IsNaN(RightPupilDiameterMm);
                if (leftValid && rightValid) return (LeftPupilDiameterMm + RightPupilDiameterMm) / 2f;
                if (leftValid) return LeftPupilDiameterMm;
                if (rightValid) return RightPupilDiameterMm;
                return float.NaN;
            }
        }
    }
}
