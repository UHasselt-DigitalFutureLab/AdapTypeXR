#nullable enable
using System.Collections.Generic;
using AdapTypeXR.Controllers;
using AdapTypeXR.Core.Events;
using AdapTypeXR.Core.Models;
using AdapTypeXR.Typography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdapTypeXR.UI
{
    /// <summary>
    /// Screen-space researcher control panel.
    /// Allows the researcher (or developer in simulation) to:
    ///   - Start / pause / resume / end a session
    ///   - Switch typography conditions manually
    ///   - Monitor current session state in real time
    ///
    /// Keyboard shortcuts (always active in simulation):
    ///   Space / → — next page
    ///   ←         — previous page
    ///   N         — next condition
    ///   P         — pause / resume
    ///   Tab       — toggle this panel's visibility
    ///
    /// All UI element references are set by the SceneBuilder editor script.
    /// </summary>
    public sealed class ResearcherControlPanel : MonoBehaviour
    {
        // ── Inspector References ───────────────────────────────────────────

        [Header("Session Controls")]
        [SerializeField] private Button? _startButton;
        [SerializeField] private Button? _pauseResumeButton;
        [SerializeField] private Button? _nextConditionButton;
        [SerializeField] private Button? _endSessionButton;

        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI? _statusText;
        [SerializeField] private TextMeshProUGUI? _conditionText;
        [SerializeField] private TextMeshProUGUI? _pageText;
        [SerializeField] private TextMeshProUGUI? _shortcutsText;

        [Header("Panel")]
        [SerializeField] private GameObject? _panelRoot;

        // ── Dependencies ───────────────────────────────────────────────────

        private ReadingSessionController? _sessionController;
        private Simulation.SimulationBootstrapper? _bootstrapper;

        // ── State ──────────────────────────────────────────────────────────

        private bool _isPaused;
        private bool _sessionActive;
        private string _currentConditionId = "—";
        private int _currentPage;
        private int _totalPages;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _sessionController = FindFirstObjectByType<ReadingSessionController>();
            _bootstrapper = FindFirstObjectByType<Simulation.SimulationBootstrapper>();

            SubscribeEvents();
            BindButtons();
            RefreshUI();
            ShowShortcutsHelp();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private void BindButtons()
        {
            _startButton?.onClick.AddListener(OnStartClicked);
            _pauseResumeButton?.onClick.AddListener(OnPauseResumeClicked);
            _nextConditionButton?.onClick.AddListener(OnNextConditionClicked);
            _endSessionButton?.onClick.AddListener(OnEndSessionClicked);
        }

        private void SubscribeEvents()
        {
            ReadingEventBus.Instance.Subscribe<SessionStartedEvent>(OnSessionStarted);
            ReadingEventBus.Instance.Subscribe<SessionEndedEvent>(OnSessionEnded);
            ReadingEventBus.Instance.Subscribe<ConditionChangedEvent>(OnConditionChanged);
            ReadingEventBus.Instance.Subscribe<PageTurnedEvent>(OnPageTurned);
        }

        private void UnsubscribeEvents()
        {
            ReadingEventBus.Instance.Unsubscribe<SessionStartedEvent>(OnSessionStarted);
            ReadingEventBus.Instance.Unsubscribe<SessionEndedEvent>(OnSessionEnded);
            ReadingEventBus.Instance.Unsubscribe<ConditionChangedEvent>(OnConditionChanged);
            ReadingEventBus.Instance.Unsubscribe<PageTurnedEvent>(OnPageTurned);
        }

        // ── Button Handlers ────────────────────────────────────────────────

        private void OnStartClicked()
        {
            if (_bootstrapper != null)
                _bootstrapper.StartDemoSession();
            else
                Debug.LogWarning("[ResearcherControlPanel] No SimulationBootstrapper found.");
        }

        private void OnPauseResumeClicked()
        {
            if (!_sessionActive || _sessionController == null) return;

            if (_isPaused)
            {
                _sessionController.ResumeSession();
                _isPaused = false;
            }
            else
            {
                _sessionController.PauseSession();
                _isPaused = true;
            }

            RefreshUI();
        }

        private void OnNextConditionClicked()
        {
            if (_sessionActive)
                _sessionController?.AdvanceCondition();
        }

        private void OnEndSessionClicked()
        {
            if (_sessionActive)
                _ = _sessionController?.EndSession();
        }

        // ── Event Handlers ─────────────────────────────────────────────────

        private void OnSessionStarted(SessionStartedEvent evt)
        {
            _sessionActive = true;
            _isPaused = false;
            _currentConditionId = "Starting…";
            RefreshUI();
        }

        private void OnSessionEnded(SessionEndedEvent evt)
        {
            _sessionActive = false;
            _isPaused = false;
            _currentConditionId = "—";
            SetStatus($"Session complete. Data saved to:\n{GetDataPath()}");
            RefreshUI();
        }

        private void OnConditionChanged(ConditionChangedEvent evt)
        {
            _currentConditionId = evt.NewConfig.DisplayName;
            RefreshUI();
        }

        private void OnPageTurned(PageTurnedEvent evt)
        {
            _currentPage = evt.ToPage + 1;
            RefreshUI();
        }

        // ── UI Refresh ─────────────────────────────────────────────────────

        private void RefreshUI()
        {
            if (_statusText != null)
            {
                if (!_sessionActive)
                    _statusText.text = "No active session";
                else if (_isPaused)
                    _statusText.text = "PAUSED";
                else
                    _statusText.text = "Recording…";
            }

            if (_conditionText != null)
                _conditionText.text = $"Condition: {_currentConditionId}";

            if (_pageText != null)
                _pageText.text = $"Page: {_currentPage}";

            if (_pauseResumeButton != null)
            {
                var label = _pauseResumeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = _isPaused ? "Resume" : "Pause";
            }

            bool hasSession = _sessionActive;
            SetButtonInteractable(_pauseResumeButton, hasSession);
            SetButtonInteractable(_nextConditionButton, hasSession && !_isPaused);
            SetButtonInteractable(_endSessionButton, hasSession);
            SetButtonInteractable(_startButton, !hasSession);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        private static void SetButtonInteractable(Button? button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void ShowShortcutsHelp()
        {
            if (_shortcutsText != null)
                _shortcutsText.text =
                    "← → Arrow keys: Page\n" +
                    "N: Next condition\n" +
                    "P: Pause / Resume\n" +
                    "Tab: Toggle panel\n" +
                    "F: Reset camera\n" +
                    "RMB + drag: Look";
        }

        // ── Keyboard Shortcuts ─────────────────────────────────────────────

        private void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.N))
                OnNextConditionClicked();

            if (Input.GetKeyDown(KeyCode.P))
                OnPauseResumeClicked();

            if (Input.GetKeyDown(KeyCode.Tab))
                TogglePanelVisibility();
        }

        private void TogglePanelVisibility()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(!_panelRoot.activeSelf);
        }

        private static string GetDataPath()
        {
#if UNITY_EDITOR
            return System.IO.Path.Combine(
                UnityEngine.Application.persistentDataPath, "Sessions");
#else
            return Application.persistentDataPath + "/Sessions";
#endif
        }
    }
}
