#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdapTypeXR.Interaction
{
    /// <summary>
    /// Allows the user to grab and reposition the book in XR/simulation.
    ///
    /// Grab modes:
    ///  - Desktop simulation: Hold G + drag mouse to move the book.
    ///  - XR (future): Will integrate with XR Interaction Toolkit's
    ///    XRGrabInteractable when the package is available.
    ///
    /// The book snaps back to its original pose when released (configurable).
    /// A Rigidbody is required on the same GameObject (set to kinematic).
    /// </summary>
    public sealed class BookInteractionController : MonoBehaviour
    {
        [Header("Grab Settings")]
        [Tooltip("Key to hold while dragging to move the book.")]
        [SerializeField] private Key _grabKey = Key.G;

        [Tooltip("Mouse sensitivity for moving the book (metres per pixel).")]
        [SerializeField] private float _moveSensitivity = 0.002f;

        [Tooltip("Mouse scroll sensitivity for pushing/pulling the book.")]
        [SerializeField] private float _depthSensitivity = 0.05f;

        [Tooltip("If true, the book returns to its start pose when released.")]
        [SerializeField] private bool _snapBackOnRelease = false;

        // ── State ────────────────────────────────────────────────────────────

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private bool _isGrabbed;
        private Vector2 _lastMousePos;

        /// <summary>True while the user is actively grabbing the book.</summary>
        public bool IsGrabbed => _isGrabbed;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;

            // Ensure Rigidbody is kinematic — we move via transform, not physics.
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            bool grabHeld = kb[_grabKey].isPressed;

            if (grabHeld && !_isGrabbed)
                BeginGrab(mouse);
            else if (!grabHeld && _isGrabbed)
                EndGrab();
            else if (_isGrabbed)
                ContinueGrab(mouse);
        }

        // ── Grab Logic ───────────────────────────────────────────────────────

        private void BeginGrab(Mouse mouse)
        {
            _isGrabbed = true;
            _lastMousePos = mouse.position.ReadValue();
        }

        private void EndGrab()
        {
            _isGrabbed = false;

            if (_snapBackOnRelease)
            {
                transform.position = _startPosition;
                transform.rotation = _startRotation;
            }
        }

        private void ContinueGrab(Mouse mouse)
        {
            var currentPos = mouse.position.ReadValue();
            var delta = currentPos - _lastMousePos;
            _lastMousePos = currentPos;

            // Lateral movement in camera-relative space.
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            Vector3 forward = cam.transform.forward;

            transform.position += right * (delta.x * _moveSensitivity)
                                + up * (delta.y * _moveSensitivity);

            // Scroll wheel pushes/pulls the book along the view direction.
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                transform.position += forward * (scroll * _depthSensitivity * Time.deltaTime);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Resets the book to its original scene-build pose.</summary>
        public void ResetPose()
        {
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _isGrabbed = false;
        }
    }
}
