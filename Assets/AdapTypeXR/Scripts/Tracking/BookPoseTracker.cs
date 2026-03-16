#nullable enable
using System;
using UnityEngine;

namespace AdapTypeXR.Tracking
{
    /// <summary>
    /// Continuously tracks the spatial relationship between the user's viewpoint
    /// (main camera / HMD) and the book, publishing samples at a configurable rate.
    ///
    /// Tracked metrics per sample:
    ///  - Distance (metres) from the camera to the book pivot
    ///  - Horizontal angle (degrees) — how far left/right the book is from gaze centre
    ///  - Vertical angle (degrees) — how far up/down the book is from gaze centre
    ///  - Whether the book is currently being grabbed
    ///
    /// Data is exposed via the <see cref="PoseSampled"/> event so that any
    /// recording pipeline (CSV, event bus, network) can consume it without coupling.
    /// </summary>
    public sealed class BookPoseTracker : MonoBehaviour
    {
        [Header("Sampling")]
        [Tooltip("Samples per second. 0 = every frame.")]
        [SerializeField, Range(0, 120)] private int _sampleRate = 30;

        // ── Events ───────────────────────────────────────────────────────────

        /// <summary>Raised each time a pose sample is taken.</summary>
        public event Action<BookPoseSample>? PoseSampled;

        // ── State ────────────────────────────────────────────────────────────

        private Transform? _cameraTransform;
        private float _sampleInterval;
        private float _timeSinceLastSample;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null)
                _cameraTransform = cam.transform;
            else
                Debug.LogWarning("[BookPoseTracker] No main camera found.");

            _sampleInterval = _sampleRate > 0 ? 1f / _sampleRate : 0f;
        }

        private void Update()
        {
            if (_cameraTransform == null) return;

            _timeSinceLastSample += Time.deltaTime;
            if (_sampleInterval > 0f && _timeSinceLastSample < _sampleInterval)
                return;

            _timeSinceLastSample = 0f;
            TakeSample();
        }

        // ── Sampling ─────────────────────────────────────────────────────────

        private void TakeSample()
        {
            if (_cameraTransform == null) return;

            Vector3 camPos = _cameraTransform.position;
            Vector3 camFwd = _cameraTransform.forward;
            Vector3 bookPos = transform.position;

            Vector3 toBook = bookPos - camPos;
            float distance = toBook.magnitude;

            // Angle between camera forward and direction to book.
            // Decompose into horizontal (yaw) and vertical (pitch) components.
            Vector3 toBookDir = toBook.normalized;

            // Project onto camera's horizontal plane for yaw angle.
            Vector3 camRight = _cameraTransform.right;
            Vector3 camUp = _cameraTransform.up;

            float horizontalAngle = Vector3.SignedAngle(
                Vector3.ProjectOnPlane(camFwd, Vector3.up),
                Vector3.ProjectOnPlane(toBookDir, Vector3.up),
                Vector3.up);

            float verticalAngle = Vector3.SignedAngle(
                Vector3.ProjectOnPlane(camFwd, camRight),
                Vector3.ProjectOnPlane(toBookDir, camRight),
                camRight);

            // Check grab state if BookInteractionController is present.
            var grabController = GetComponent<Interaction.BookInteractionController>();
            bool isGrabbed = grabController != null && grabController.IsGrabbed;

            var sample = new BookPoseSample(
                timestamp: Time.time,
                distance: distance,
                horizontalAngleDeg: horizontalAngle,
                verticalAngleDeg: verticalAngle,
                bookWorldPosition: bookPos,
                bookWorldRotation: transform.rotation.eulerAngles,
                cameraWorldPosition: camPos,
                cameraForward: camFwd,
                isGrabbed: isGrabbed);

            PoseSampled?.Invoke(sample);
        }
    }

    /// <summary>
    /// A single sample of the book's pose relative to the user's viewpoint.
    /// </summary>
    public readonly struct BookPoseSample
    {
        /// <summary>Time.time when this sample was taken.</summary>
        public float Timestamp { get; }

        /// <summary>Euclidean distance in metres from camera to book pivot.</summary>
        public float Distance { get; }

        /// <summary>Horizontal (yaw) angle in degrees. Positive = book is to the right.</summary>
        public float HorizontalAngleDeg { get; }

        /// <summary>Vertical (pitch) angle in degrees. Positive = book is above gaze centre.</summary>
        public float VerticalAngleDeg { get; }

        /// <summary>Book world position.</summary>
        public Vector3 BookWorldPosition { get; }

        /// <summary>Book world rotation (euler angles).</summary>
        public Vector3 BookWorldRotation { get; }

        /// <summary>Camera world position at sample time.</summary>
        public Vector3 CameraWorldPosition { get; }

        /// <summary>Camera forward direction at sample time.</summary>
        public Vector3 CameraForward { get; }

        /// <summary>True if the user was grabbing the book at sample time.</summary>
        public bool IsGrabbed { get; }

        public BookPoseSample(
            float timestamp, float distance,
            float horizontalAngleDeg, float verticalAngleDeg,
            Vector3 bookWorldPosition, Vector3 bookWorldRotation,
            Vector3 cameraWorldPosition, Vector3 cameraForward,
            bool isGrabbed)
        {
            Timestamp = timestamp;
            Distance = distance;
            HorizontalAngleDeg = horizontalAngleDeg;
            VerticalAngleDeg = verticalAngleDeg;
            BookWorldPosition = bookWorldPosition;
            BookWorldRotation = bookWorldRotation;
            CameraWorldPosition = cameraWorldPosition;
            CameraForward = cameraForward;
            IsGrabbed = isGrabbed;
        }

        public override string ToString() =>
            $"[{Timestamp:F2}s] dist={Distance:F3}m h={HorizontalAngleDeg:F1}° v={VerticalAngleDeg:F1}° grabbed={IsGrabbed}";
    }
}
