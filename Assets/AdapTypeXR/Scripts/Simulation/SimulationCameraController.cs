#nullable enable
using UnityEngine;

namespace AdapTypeXR.Simulation
{
    /// <summary>
    /// Minimal first-person camera controller for desktop simulation.
    /// Allows the researcher/developer to look around the scene and position
    /// themselves relative to the book without a headset.
    ///
    /// Controls:
    ///   Right-click + drag — rotate camera (mouse look)
    ///   W / A / S / D      — move forward / left / back / right
    ///   Q / E              — move down / up
    ///   Scroll wheel       — move forward / back faster
    ///   F                  — snap to default reading position
    /// </summary>
    public sealed class SimulationCameraController : MonoBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float _mouseSensitivity = 2.0f;
        [SerializeField] private float _pitchClampDeg = 80f;

        [Header("Move Settings")]
        [SerializeField] private float _moveSpeed = 1.5f;
        [SerializeField] private float _scrollSpeed = 3.0f;
        [SerializeField] private float _fastMultiplier = 3.0f;

        [Header("Default Reading Position")]
        [Tooltip("Position to snap to when pressing F.")]
        [SerializeField] private Vector3 _defaultPosition = new(0f, 1.7f, 0f);
        [SerializeField] private Vector3 _defaultEulerAngles = new(0f, 0f, 0f);

        // ── State ──────────────────────────────────────────────────────────

        private float _yaw;
        private float _pitch;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            SnapToDefault();
        }

        private void Update()
        {
            HandleMouseLook();
            HandleKeyboardMove();
            HandleScrollMove();

            if (Input.GetKeyDown(KeyCode.F))
                SnapToDefault();
        }

        // ── Private ────────────────────────────────────────────────────────

        private void HandleMouseLook()
        {
            // Only look while right mouse button is held.
            if (!Input.GetMouseButton(1)) return;

            _yaw += Input.GetAxis("Mouse X") * _mouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * _mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -_pitchClampDeg, _pitchClampDeg);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleKeyboardMove()
        {
            float speed = _moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= _fastMultiplier;

            var move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

            transform.position += move * speed;
        }

        private void HandleScrollMove()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                transform.position += transform.forward * (scroll * _scrollSpeed);
        }

        private void SnapToDefault()
        {
            transform.position = _defaultPosition;
            transform.eulerAngles = _defaultEulerAngles;
            _yaw = _defaultEulerAngles.y;
            _pitch = _defaultEulerAngles.x;
        }
    }
}
