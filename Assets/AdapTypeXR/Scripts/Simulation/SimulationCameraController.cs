#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdapTypeXR.Simulation
{
    /// <summary>
    /// First-person camera controller for desktop simulation, designed
    /// to be fully navigable via keyboard, mouse, and touchpad.
    ///
    /// Controls — Look:
    ///   Right-click + drag  — rotate camera (mouse look)
    ///   V                   — toggle free-look mode (no click needed)
    ///   I / K               — look up / down (keyboard-only)
    ///   J / L               — look left / right (keyboard-only)
    ///
    /// Controls — Move:
    ///   W / A / S / D       — move forward / left / back / right
    ///   Q / E               — move down / up
    ///   Scroll wheel        — dolly forward / back (touchpad two-finger scroll)
    ///   Left Shift          — move faster
    ///   F                   — snap to default reading position
    /// </summary>
    public sealed class SimulationCameraController : MonoBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float _mouseSensitivity = 0.15f;
        [SerializeField] private float _keyboardLookSpeed = 90f;
        [SerializeField] private float _pitchClampDeg = 80f;

        [Header("Move Settings")]
        [SerializeField] private float _moveSpeed = 1.5f;
        [SerializeField] private float _scrollSpeed = 3.0f;
        [SerializeField] private float _fastMultiplier = 3.0f;

        [Header("Smoothing")]
        [Tooltip("Interpolation speed for movement. Higher = snappier, lower = smoother.")]
        [SerializeField] private float _moveSmoothTime = 12f;

        [Header("Default Reading Position")]
        [SerializeField] private Vector3 _defaultPosition = new(0f, 1.7f, 0f);
        [SerializeField] private Vector3 _defaultEulerAngles = new(0f, 0f, 0f);

        private float _yaw;
        private float _pitch;
        private bool _freeLook;
        private Vector3 _moveVelocity;

        private void Start() => SnapToDefault();

        private void Update()
        {
            var kb = Keyboard.current;

            HandleFreeLookToggle(kb);
            HandleMouseLook();
            HandleKeyboardLook(kb);
            HandleKeyboardMove(kb);
            HandleScrollMove();

            if (kb != null && kb.fKey.wasPressedThisFrame)
                SnapToDefault();
        }

        // ── Free-Look Toggle ─────────────────────────────────────────────────

        private void HandleFreeLookToggle(Keyboard? kb)
        {
            if (kb == null) return;
            if (kb.vKey.wasPressedThisFrame)
            {
                _freeLook = !_freeLook;
                Debug.Log($"[Camera] Free-look {(_freeLook ? "ON" : "OFF")}");
            }
        }

        // ── Mouse Look ──────────────────────────────────────────────────────

        private void HandleMouseLook()
        {
            if (Mouse.current == null) return;

            // Activate on right-click drag or when free-look is on.
            bool active = _freeLook || Mouse.current.rightButton.isPressed;
            if (!active) return;

            var delta = Mouse.current.delta.ReadValue();
            ApplyLookDelta(delta.x * _mouseSensitivity, -delta.y * _mouseSensitivity);
        }

        // ── Keyboard Look (I/J/K/L) ─────────────────────────────────────────

        private void HandleKeyboardLook(Keyboard? kb)
        {
            if (kb == null) return;

            float dt = Time.deltaTime;
            float speed = _keyboardLookSpeed * dt;

            float dYaw = 0f;
            float dPitch = 0f;

            if (kb.jKey.isPressed) dYaw -= speed;
            if (kb.lKey.isPressed) dYaw += speed;
            if (kb.iKey.isPressed) dPitch -= speed;   // look up (pitch decreases)
            if (kb.kKey.isPressed) dPitch += speed;    // look down

            if (dYaw != 0f || dPitch != 0f)
                ApplyLookDelta(dYaw, dPitch);
        }

        private void ApplyLookDelta(float deltaYaw, float deltaPitch)
        {
            _yaw += deltaYaw;
            _pitch += deltaPitch;
            _pitch = Mathf.Clamp(_pitch, -_pitchClampDeg, _pitchClampDeg);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        // ── Keyboard Move (WASD/QE) ──────────────────────────────────────────

        private void HandleKeyboardMove(Keyboard? kb)
        {
            if (kb == null) return;

            float speed = _moveSpeed;
            if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                speed *= _fastMultiplier;

            var target = Vector3.zero;
            if (kb.wKey.isPressed) target += transform.forward;
            if (kb.sKey.isPressed) target -= transform.forward;
            if (kb.aKey.isPressed) target -= transform.right;
            if (kb.dKey.isPressed) target += transform.right;
            if (kb.eKey.isPressed) target += Vector3.up;
            if (kb.qKey.isPressed) target -= Vector3.up;

            target *= speed;

            // Smooth interpolation for comfortable movement.
            _moveVelocity = Vector3.Lerp(_moveVelocity, target, _moveSmoothTime * Time.deltaTime);
            transform.position += _moveVelocity * Time.deltaTime;
        }

        // ── Scroll / Touchpad Move ───────────────────────────────────────────

        private void HandleScrollMove()
        {
            if (Mouse.current == null) return;
            var scroll = Mouse.current.scroll.ReadValue();

            // Vertical scroll = dolly forward/back (two-finger swipe on touchpad).
            if (Mathf.Abs(scroll.y) > 0.001f)
                transform.position += transform.forward * (scroll.y * _scrollSpeed * 0.01f);

            // Horizontal scroll = strafe left/right (two-finger horizontal swipe on touchpad).
            if (Mathf.Abs(scroll.x) > 0.001f)
                transform.position += transform.right * (scroll.x * _scrollSpeed * 0.01f);
        }

        // ── Reset ────────────────────────────────────────────────────────────

        private void SnapToDefault()
        {
            transform.position = _defaultPosition;
            transform.eulerAngles = _defaultEulerAngles;
            _yaw = _defaultEulerAngles.y;
            _pitch = _defaultEulerAngles.x;
            _moveVelocity = Vector3.zero;
            _freeLook = false;
        }
    }
}
