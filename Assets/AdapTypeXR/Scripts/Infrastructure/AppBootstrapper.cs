#nullable enable
using System.Collections.Generic;
using AdapTypeXR.Controllers;
using AdapTypeXR.Core;
using AdapTypeXR.Core.Models;
using AdapTypeXR.Presenters;
using AdapTypeXR.Repositories;
using AdapTypeXR.Services;
using AdapTypeXR.Typography;
using UnityEngine;

namespace AdapTypeXR.Infrastructure
{
    /// <summary>
    /// Wires all application dependencies at startup.
    ///
    /// This is the Composition Root — the single place where concrete
    /// implementations are selected and injected into their consumers.
    /// No other class instantiates services or makes environment decisions.
    ///
    /// Swap VarjoEyeTrackingService ↔ MockEyeTrackingService here depending
    /// on whether a Varjo headset is available (controlled by inspector toggle).
    /// </summary>
    public sealed class AppBootstrapper : MonoBehaviour
    {
        // ── Inspector Wiring ───────────────────────────────────────────────

        [Header("Scene References")]
        [SerializeField] private ReadingSessionController _sessionController = null!;
        [SerializeField] private BookPresenter _bookPresenter = null!;

        [Header("Eye Tracking")]
        [Tooltip("Enable to use real Varjo eye tracking. Disable to use mouse-based mock.")]
        [SerializeField] private bool _useVarjoEyeTracking = true;

        [SerializeField] private VarjoEyeTrackingService _varjoEyeTracker = null!;
        [SerializeField] private MockEyeTrackingService _mockEyeTracker = null!;

        [Header("Data Storage")]
        [Tooltip("Override data directory. Leave empty to use Application.persistentDataPath/Sessions.")]
        [SerializeField] private string _dataDirectoryOverride = string.Empty;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private CsvDataCollectionRepository? _repository;

        private void Awake()
        {
            var eyeTracker = _useVarjoEyeTracking
                ? (Core.Interfaces.IEyeTrackingService)_varjoEyeTracker
                : _mockEyeTracker;

            // Disable whichever service is not in use.
            _varjoEyeTracker.gameObject.SetActive(_useVarjoEyeTracking);
            _mockEyeTracker.gameObject.SetActive(!_useVarjoEyeTracking);

            _repository = string.IsNullOrEmpty(_dataDirectoryOverride)
                ? new CsvDataCollectionRepository()
                : new CsvDataCollectionRepository(_dataDirectoryOverride);

            _sessionController.Inject(eyeTracker, _repository, _bookPresenter);

            Debug.Log($"[AppBootstrapper] Wired: eyeTracker={eyeTracker.GetType().Name}, " +
                $"repository=CsvDataCollectionRepository, " +
                $"bookPresenter={_bookPresenter.GetType().Name}");
        }

        private void OnDestroy()
        {
            _repository?.Dispose();
        }

        // ── Development Helpers ────────────────────────────────────────────

#if UNITY_EDITOR
        [Header("Editor Quick-Start")]
        [Tooltip("Auto-start a demo session on Play in the editor.")]
        [SerializeField] private bool _autoStartDemoSession = false;

        private void Start()
        {
            if (!_autoStartDemoSession) return;

            var conditions = new List<TypographyConfig>(FontProfileFactory.BuildDefaultCatalogue());
            var passage = PassageLibrary.TheRoadNotTaken();

            _sessionController.BeginSession(
                participantId: "DEMO_001",
                profile: NeurodivergentProfile.Neurotypical,
                conditions: conditions,
                passage: passage,
                ipd: 64f,
                gazeConsented: true,
                physiologicalConsented: true);

            Debug.Log("[AppBootstrapper] Demo session auto-started.");
        }


#endif
    }
}
