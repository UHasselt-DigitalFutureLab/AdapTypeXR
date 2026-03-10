using System;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdapTypeXR.Services
{
    /// <summary>
    /// Simulates eye tracking data for editor testing and CI environments
    /// where a Varjo XR-4 headset is not available.
    ///
    /// Simulates realistic gaze behaviour:
    /// - Gaze follows mouse position (editor) or a simple sweep pattern (build)
    /// - Pupil dilation oscillates to simulate cognitive load variation
    /// - Occasional blinks (configurable rate)
    ///
    /// Design pattern: Adapter — same interface as VarjoEyeTrackingService,
    /// swappable via AppBootstrapper with zero changes to consumers.
    /// </summary>
    public sealed class MockEyeTrackingService : MonoBehaviour, IEyeTrackingService
    {
        // ── Configuration ──────────────────────────────────────────────────

        [Header("Simulation Parameters")]
        [SerializeField, Range(30, 200)] private int _sampleRateHz = 60;
        [SerializeField, Range(0f, 1f)] private float _blinkProbabilityPerSecond = 0.3f;
        [SerializeField] private float _basePupilDiameterMm = 3.5f;
        [SerializeField] private float _pupilDilationAmplitudeMm = 0.8f;
        [SerializeField] private float _pupilDilationFrequencyHz = 0.1f;

        [Tooltip("Camera used to project mouse position into gaze direction.")]
        [SerializeField] private Camera? _gazeCamera;

        // ── IEyeTrackingService ────────────────────────────────────────────

        /// <inheritdoc />
        public event Action<GazeDataPoint>? GazeSampleReceived;

        /// <inheritdoc />
        public bool IsTracking { get; private set; }

        // ── Private State ──────────────────────────────────────────────────

        private GazeDataPoint? _latestGaze;
        private float _sampleInterval;
        private float _timeSinceLastSample;
        private float _simulationTime;
        private string _sessionId = string.Empty;
        private string _conditionId = string.Empty;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _sampleInterval = 1f / _sampleRateHz;
            if (_gazeCamera == null)
                _gazeCamera = Camera.main;
        }

        private void Update()
        {
            if (!IsTracking) return;

            _simulationTime += Time.deltaTime;
            _timeSinceLastSample += Time.deltaTime;

            if (_timeSinceLastSample < _sampleInterval) return;
            _timeSinceLastSample = 0f;
            SampleGaze();
        }

        // ── IEyeTrackingService Implementation ─────────────────────────────

        /// <inheritdoc />
        public void StartTracking()
        {
            IsTracking = true;
            Debug.Log("[MockEyeTrackingService] Mock eye tracking started.");
        }

        /// <inheritdoc />
        public void StopTracking()
        {
            IsTracking = false;
            _latestGaze = null;
            Debug.Log("[MockEyeTrackingService] Mock eye tracking stopped.");
        }

        /// <inheritdoc />
        public GazeDataPoint? GetLatestGaze() => _latestGaze;

        /// <summary>Sets the active session and condition context for stamping on samples.</summary>
        public void SetContext(string sessionId, string conditionId)
        {
            _sessionId = sessionId;
            _conditionId = conditionId;
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private void SampleGaze()
        {
            if (_gazeCamera == null) return;

            // Use mouse position as gaze target in the editor.
            var screenPos = Input.mousePosition;
            var gazeRay = _gazeCamera.ScreenPointToRay(screenPos);

            Vector3? hitPoint = null;
            string? hitObjectId = null;

            if (Physics.Raycast(gazeRay, out var hit, 10f))
            {
                hitPoint = hit.point;
                hitObjectId = hit.collider.gameObject.GetInstanceID().ToString();
            }

            // Simulate pupil dilation with a sine oscillation.
            float pupilDiameter = _basePupilDiameterMm +
                _pupilDilationAmplitudeMm * Mathf.Sin(2f * Mathf.PI * _pupilDilationFrequencyHz * _simulationTime);

            // Simulate occasional blinks.
            bool isBlinking = Random.value < _blinkProbabilityPerSecond * _sampleInterval;
            float eyeOpenness = isBlinking ? 0f : 1f;

            var point = new GazeDataPoint(
                timestamp: DateTime.UtcNow,
                gazeOrigin: gazeRay.origin,
                gazeDirection: gazeRay.direction,
                hitPoint: hitPoint,
                hitObjectId: hitObjectId,
                leftPupilDiameterMm: pupilDiameter,
                rightPupilDiameterMm: pupilDiameter,
                leftEyeOpenness: eyeOpenness,
                rightEyeOpenness: eyeOpenness,
                sessionId: _sessionId,
                conditionId: _conditionId
            );

            _latestGaze = point;
            GazeSampleReceived?.Invoke(point);
        }
    }
}
