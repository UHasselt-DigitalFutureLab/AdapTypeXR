using AdapTypeXR.Services;
using UnityEngine;

namespace AdapTypeXR.Simulation
{
    /// <summary>
    /// Renders a gaze ray and fixation indicator in the scene view and game view
    /// during simulation, so researchers can see what the mock eye tracker is doing.
    ///
    /// Attach to the same GameObject as <see cref="MockEyeTrackingService"/>.
    /// The indicator appears as a small sphere at the gaze hit point.
    /// </summary>
    [RequireComponent(typeof(MockEyeTrackingService))]
    public sealed class GazeDebugVisualizer : MonoBehaviour
    {
        [Header("Visualizer")]
        [SerializeField] private Color _gazeRayColour = new(0f, 1f, 0.5f, 0.8f);
        [SerializeField] private Color _hitColour = new(1f, 0.3f, 0f, 1f);
        [SerializeField] private float _hitSphereRadius = 0.015f;
        [SerializeField] private bool _showInGameView = true;

        [Header("HUD")]
        [SerializeField] private bool _showGazeHud = true;

        // ── State ──────────────────────────────────────────────────────────

        private MockEyeTrackingService? _eyeTracker;
        private Vector3 _lastHitPoint;
        private bool _hasHit;
        private float _lastPupilDiameter;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _eyeTracker = GetComponent<MockEyeTrackingService>();
        }

        private void Update()
        {
            if (_eyeTracker == null || !_eyeTracker.IsTracking) return;

            var gaze = _eyeTracker.GetLatestGaze();
            if (gaze == null) return;

            _hasHit = gaze.HitPoint.HasValue;
            _lastHitPoint = gaze.HitPoint ?? Vector3.zero;
            _lastPupilDiameter = gaze.MeanPupilDiameterMm;

            // Draw gaze ray in scene view (editor only).
            Debug.DrawRay(gaze.GazeOrigin, gaze.GazeDirection * 5f, _gazeRayColour);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_hasHit) return;

            Gizmos.color = _hitColour;
            Gizmos.DrawSphere(_lastHitPoint, _hitSphereRadius);
        }

        private void OnGUI()
        {
            if (!_showGazeHud || !Application.isPlaying) return;

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = Color.white;

            string status = _eyeTracker?.IsTracking == true ? "Tracking" : "Idle";
            string hit = _hasHit ? "YES" : "NO";
            string pupil = float.IsNaN(_lastPupilDiameter)
                ? "N/A"
                : $"{_lastPupilDiameter:F2} mm";

            GUI.Box(new Rect(10, 10, 220, 80),
                $"[Mock Eye Tracker]\n" +
                $"Status: {status}\n" +
                $"Gaze hit: {hit}\n" +
                $"Pupil Ø: {pupil}",
                style);
        }
    }
}
