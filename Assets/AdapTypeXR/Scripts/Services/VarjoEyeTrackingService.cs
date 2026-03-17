#nullable enable
using System;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using UnityEngine;

using Varjo.XR;

namespace AdapTypeXR.Services
{
    /// <summary>
    /// Adapter wrapping the Varjo XR SDK eye tracking API.
    /// Translates Varjo-specific gaze data into the domain type <see cref="GazeDataPoint"/>.
    ///
    /// Design pattern: Adapter — insulates the domain from Varjo SDK changes.
    ///
    /// The VARJO_XR preprocessor symbol must be defined in Project Settings > Player
    /// when building for the Varjo XR-4. Without it, this class compiles to a no-op
    /// so the project remains buildable on non-Varjo machines.
    /// </summary>
    public sealed class VarjoEyeTrackingService : MonoBehaviour, IEyeTrackingService
    {
        // ── Configuration ──────────────────────────────────────────────────

        [Tooltip("ID of the current reading session. Set by ReadingSessionController.")]
        [SerializeField] private string _sessionId = string.Empty;

        [Tooltip("ID of the active typography condition. Set by ReadingSessionController.")]
        [SerializeField] private string _conditionId = string.Empty;

        [Tooltip("Sample rate in Hz. Varjo XR-4 supports up to 200 Hz.")]
        [SerializeField, Range(30, 200)] private int _targetSampleRateHz = 200;

        // ── IEyeTrackingService ────────────────────────────────────────────

        /// <inheritdoc />
        public event Action<GazeDataPoint>? GazeSampleReceived;

        /// <inheritdoc />
        public bool IsTracking { get; private set; }

        // ── Private State ──────────────────────────────────────────────────

        private GazeDataPoint? _latestGaze;
        private float _sampleInterval;
        private float _timeSinceLastSample;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _sampleInterval = 1f / _targetSampleRateHz;
        }

        private void Update()
        {
            if (!IsTracking) return;

            _timeSinceLastSample += Time.deltaTime;
            if (_timeSinceLastSample < _sampleInterval) return;

            _timeSinceLastSample = 0f;
            SampleGaze();
        }

        // ── IEyeTrackingService Implementation ─────────────────────────────

        /// <inheritdoc />
        public void StartTracking()
        {
#if VARJO_XR
            if (!VarjoEyeTracking.IsGazeAllowed())
            {
                Debug.LogWarning("[VarjoEyeTrackingService] Gaze is not allowed. " +
                    "Check Varjo Base settings and ensure eye tracking is enabled.");
                return;
            }

            if (!VarjoEyeTracking.IsGazeCalibrated())
            {
                Debug.LogWarning("[VarjoEyeTrackingService] Eye tracker is not calibrated. " +
                    "Trigger calibration via Varjo Base before starting a session.");
            }

            IsTracking = true;
            Debug.Log("[VarjoEyeTrackingService] Eye tracking started.");
#else
            Debug.LogWarning("[VarjoEyeTrackingService] VARJO_XR symbol not defined. " +
                "Use MockEyeTrackingService for editor testing.");
#endif
        }

        /// <inheritdoc />
        public void StopTracking()
        {
            IsTracking = false;
            _latestGaze = null;
            Debug.Log("[VarjoEyeTrackingService] Eye tracking stopped.");
        }

        /// <inheritdoc />
        public GazeDataPoint? GetLatestGaze() => _latestGaze;

        /// <summary>
        /// Sets the active session and condition IDs so they are stamped on gaze samples.
        /// Called by <see cref="Controllers.ReadingSessionController"/> when state changes.
        /// </summary>
        public void SetContext(string sessionId, string conditionId)
        {
            _sessionId = sessionId;
            _conditionId = conditionId;
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private void SampleGaze()
        {
#if VARJO_XR
            var eyeData = VarjoEyeTracking.GetGaze();

            if (eyeData.status == VarjoEyeTracking.GazeStatus.Invalid) return;

            // Perform a raycast to find what the user is looking at.
            var gazeRay = new Ray(
                transform.TransformPoint(eyeData.gaze.origin),
                transform.TransformDirection(eyeData.gaze.forward));

            Vector3? hitPoint = null;
            string? hitObjectId = null;

            if (Physics.Raycast(gazeRay, out var hit, 5f))
            {
                hitPoint = hit.point;
                hitObjectId = hit.collider.gameObject.GetInstanceID().ToString();
            }

            float leftPupil = eyeData.leftStatus == VarjoEyeTracking.GazeEyeStatus.Compensated
                ? eyeData.leftPupilSize : float.NaN;
            float rightPupil = eyeData.rightStatus == VarjoEyeTracking.GazeEyeStatus.Compensated
                ? eyeData.rightPupilSize : float.NaN;

            // VarjoEyeTracking does not expose openness directly; derive from blink status.
            float leftOpenness = eyeData.leftStatus == VarjoEyeTracking.GazeEyeStatus.Compensated
                ? 1f : 0f;
            float rightOpenness = eyeData.rightStatus == VarjoEyeTracking.GazeEyeStatus.Compensated
                ? 1f : 0f;

            var point = new GazeDataPoint(
                timestamp: DateTime.UtcNow,
                gazeOrigin: gazeRay.origin,
                gazeDirection: gazeRay.direction,
                hitPoint: hitPoint,
                hitObjectId: hitObjectId,
                leftPupilDiameterMm: leftPupil,
                rightPupilDiameterMm: rightPupil,
                leftEyeOpenness: leftOpenness,
                rightEyeOpenness: rightOpenness,
                sessionId: _sessionId,
                conditionId: _conditionId
            );

            _latestGaze = point;
            GazeSampleReceived?.Invoke(point);
#endif
        }
    }
}
