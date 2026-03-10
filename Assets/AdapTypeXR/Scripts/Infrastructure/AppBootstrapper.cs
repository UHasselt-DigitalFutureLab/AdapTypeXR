using System.Collections.Generic;
using AdapTypeXR.Controllers;
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
            var passage = CreateDemoPassage();

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

        private static ReadingPassage CreateDemoPassage()
        {
            const string text =
                "The ancient library stood at the edge of the city, its stone walls worn smooth " +
                "by centuries of wind and rain. Inside, the smell of old paper and dust filled " +
                "the air. Rows of wooden shelves stretched from floor to ceiling, each one heavy " +
                "with books of every size and colour. A young researcher moved quietly through the " +
                "aisles, her fingers trailing along the spines as she searched for a particular volume. " +
                "She had been told that somewhere in this collection was a manuscript describing a " +
                "method for decoding ancient symbols. Whether that was true, she could not yet say.";

            var pages = new List<string> { text };
            var questions = new List<ComprehensionQuestion>
            {
                new("Q1", "Where was the library located?", QuestionType.CuedRecall,
                    new[] { "edge", "city" }, 1f),
                new("Q2", "What was the researcher looking for?", QuestionType.CuedRecall,
                    new[] { "manuscript", "symbols", "decoding" }, 2f),
                new("Q3", "What can you infer about the library's age?", QuestionType.Inference,
                    new[] { "ancient", "centuries", "stone", "old" }, 2f),
            };

            return new ReadingPassage(
                passageId: "DEMO_P001",
                title: "The Ancient Library",
                fullText: text,
                pages: pages,
                wordCount: text.Split(' ').Length,
                fleschKincaidGradeLevel: 7.2f,
                questions: questions);
        }
#endif
    }
}
