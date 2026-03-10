#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdapTypeXR.Simulation
{
    /// <summary>
    /// Minimal first-person camera controller for desktop simulation.
    /// Uses the Input System package (com.unity.inputsystem).
    ///
    /// Controls:
    ///   Right-click + drag — rotate camera (mouse look)
    ///   W / A / S / D      — move forward / left / back / right
    ///   Q / E              — move down / up
    ///   Scroll wheel       — dolly forward / back
    ///   Left Shift         — move faster
    ///   F                  — snap to default reading position
    /// </summary>
    public sealed class SimulationCameraController : MonoBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float _mouseSensitivity = 0.15f;
        [SerializeField] private float _pitchClampDeg = 80f;

        [Header("Move Settings")]
        [SerializeField] private float _moveSpeed = 1.5f;
        [SerializeField] private float _scrollSpeed = 3.0f;
        [SerializeField] private float _fastMultiplier = 3.0f;

        [Header("Default Reading Position")]
        [SerializeField] private Vector3 _defaultPosition = new(0f, 1.7f, 0f);
        [SerializeField] private Vector3 _defaultEulerAngles = new(0f, 0f, 0f);

        private float _yaw;
        private float _pitch;

        private void Start() => SnapToDefault();

        private void Update()
        {
            HandleMouseLook();
            HandleKeyboardMove();
            HandleScrollMove();

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                SnapToDefault();
        }

        private void HandleMouseLook()
        {
            if (Mouse.current == null) return;
            if (!Mouse.current.rightButton.isPressed) return;

            var delta = Mouse.current.delta.ReadValue();
            _yaw   += delta.x * _mouseSensitivity;
            _pitch -= delta.y * _mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -_pitchClampDeg, _pitchClampDeg);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleKeyboardMove()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float speed = _moveSpeed * Time.deltaTime;
            if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                speed *= _fastMultiplier;

            var move = Vector3.zero;
            if (kb.wKey.isPressed) move += transform.forward;
            if (kb.sKey.isPressed) move -= transform.forward;
            if (kb.aKey.isPressed) move -= transform.right;
            if (kb.dKey.isPressed) move += transform.right;
            if (kb.eKey.isPressed) move += Vector3.up;
            if (kb.qKey.isPressed) move -= Vector3.up;

            transform.position += move * speed;
        }

        private void HandleScrollMove()
        {
            if (Mouse.current == null) return;
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.001f)
                transform.position += transform.forward * (scroll * _scrollSpeed * 0.01f);
        }

        private void SnapToDefault()
        {
            transform.position = _defaultPosition;
            transform.eulerAngles = _defaultEulerAngles;
            _yaw   = _defaultEulerAngles.y;
            _pitch = _defaultEulerAngles.x;
        }
    }
}
