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

namespace AdapTypeXR.Simulation
{
    /// <summary>
    /// Composition root for desktop simulation sessions.
    /// Wires all dependencies at runtime by auto-discovering scene components,
    /// then starts a demo session with all Sprint 0 typography conditions.
    ///
    /// This replaces AppBootstrapper for non-XR simulation runs. It is designed
    /// to work out-of-the-box without any inspector wiring after scene creation.
    ///
    /// Usage: Add to a GameObject in the scene. Press Play.
    /// </summary>
    public sealed class SimulationBootstrapper : MonoBehaviour
    {
        [Header("Simulation Settings")]
        [Tooltip("Participant ID written to session data.")]
        [SerializeField] private string _participantId = "SIM_001";

        [Tooltip("Neurodivergent profile for the demo session.")]
        [SerializeField] private NeurodivergentProfile _profile = NeurodivergentProfile.Neurotypical;

        [Tooltip("Reading speed in WPM for animated conditions.")]
        [SerializeField, Range(100, 500)] private float _wordsPerMinute = 250f;

        [Tooltip("If true, starts a session automatically on Play.")]
        [SerializeField] private bool _autoStart = true;

        // ── Wired at Runtime ───────────────────────────────────────────────

        private ReadingSessionController? _sessionController;
        private CsvDataCollectionRepository? _repository;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            WireDependencies();
        }

        private void Start()
        {
            if (_autoStart)
                StartDemoSession();
        }

        private void OnDestroy()
        {
            _repository?.Dispose();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Starts a demo reading session with all Sprint 0 conditions.
        /// Safe to call from UI buttons.
        /// </summary>
        public void StartDemoSession()
        {
            if (_sessionController == null)
            {
                Debug.LogError("[SimulationBootstrapper] SessionController not found.");
                return;
            }

            var conditions = BuildConditions();
            var passage = PassageLibrary.TheRoadNotTaken();

            _sessionController.BeginSession(
                participantId: _participantId,
                profile: _profile,
                conditions: conditions,
                passage: passage,
                ipd: 64f,
                gazeConsented: true,
                physiologicalConsented: true);

            Debug.Log($"[SimulationBootstrapper] Demo session started — " +
                $"{conditions.Count} conditions, participant: {_participantId}");
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private void WireDependencies()
        {
            _sessionController = FindFirstObjectByType<ReadingSessionController>();
            if (_sessionController == null)
            {
                Debug.LogError("[SimulationBootstrapper] ReadingSessionController not found in scene.");
                return;
            }

            var eyeTracker = FindFirstObjectByType<MockEyeTrackingService>();
            if (eyeTracker == null)
            {
                Debug.LogError("[SimulationBootstrapper] MockEyeTrackingService not found in scene.");
                return;
            }

            var bookPresenter = FindFirstObjectByType<BookPresenter>();
            if (bookPresenter == null)
            {
                Debug.LogError("[SimulationBootstrapper] BookPresenter not found in scene.");
                return;
            }

            _repository = new CsvDataCollectionRepository();
            _sessionController.Inject(eyeTracker, _repository, bookPresenter);

            Debug.Log("[SimulationBootstrapper] Dependencies wired successfully.");
        }

        private List<TypographyConfig> BuildConditions()
        {
            // Apply custom WPM to animated conditions.
            var catalogue = new List<TypographyConfig>(FontProfileFactory.BuildDefaultCatalogue());
            foreach (var config in catalogue)
                if (config.Animation != AnimationMode.None)
                    config.WordsPerMinute = _wordsPerMinute;
            return catalogue;
        }

    }
}
