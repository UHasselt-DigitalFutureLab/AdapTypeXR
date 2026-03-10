using System;
using System.Collections.Generic;
using AdapTypeXR.Core.Events;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using AdapTypeXR.Typography;
using UnityEngine;

namespace AdapTypeXR.Controllers
{
    /// <summary>
    /// Orchestrates a complete reading study session.
    ///
    /// Responsibilities (Single Responsibility: session lifecycle management):
    /// - State machine: Idle → Active → Paused → Completed
    /// - Connects eye tracking, data repository, and book presenter
    /// - Cycles through typography conditions in the configured order
    /// - Triggers data recording and publishes domain events
    ///
    /// This is the primary Controller in GRASP terms — it handles system-level
    /// events and delegates to specialised classes for all concrete work.
    ///
    /// All dependencies are injected via <see cref="Inject"/> to support
    /// testability and comply with the Dependency Inversion Principle.
    /// </summary>
    public sealed class ReadingSessionController : MonoBehaviour
    {
        // ── State Machine ──────────────────────────────────────────────────

        private enum SessionState { Idle, Active, Paused, Completed }
        private SessionState _state = SessionState.Idle;

        // ── Dependencies (injected) ────────────────────────────────────────

        private IEyeTrackingService? _eyeTracker;
        private IDataCollectionRepository? _repository;
        private IBookPresenter? _bookPresenter;

        // ── Session State ──────────────────────────────────────────────────

        private ReadingSession? _activeSession;
        private List<TypographyConfig> _conditions = new();
        private int _conditionIndex;
        private ReadingPassage? _activePassage;
        private DateTime _passageStartTime;

        // ── Inspector ──────────────────────────────────────────────────────

        [Header("Session Defaults")]
        [SerializeField] private string _appVersion = "0.1.0-sprint0";

        [Header("Debug")]
        [SerializeField] private bool _logStateTransitions = true;

        // ── Dependency Injection ───────────────────────────────────────────

        /// <summary>
        /// Injects all required services. Called by <see cref="Infrastructure.AppBootstrapper"/>.
        /// </summary>
        public void Inject(
            IEyeTrackingService eyeTracker,
            IDataCollectionRepository repository,
            IBookPresenter bookPresenter)
        {
            _eyeTracker = eyeTracker;
            _repository = repository;
            _bookPresenter = bookPresenter;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ── Public Session API ─────────────────────────────────────────────

        /// <summary>
        /// Begins a new reading session for the given participant.
        /// </summary>
        /// <param name="participantId">Anonymised participant ID.</param>
        /// <param name="profile">Neurodivergent profile.</param>
        /// <param name="conditions">Ordered list of typography conditions to present.</param>
        /// <param name="passage">The reading passage to display.</param>
        /// <param name="ipd">Interpupillary distance in mm from Varjo calibration.</param>
        /// <param name="gazeConsented">Whether gaze recording is consented.</param>
        /// <param name="physiologicalConsented">Whether physiological data recording is consented.</param>
        public async void BeginSession(
            string participantId,
            NeurodivergentProfile profile,
            List<TypographyConfig> conditions,
            ReadingPassage passage,
            float ipd = 64f,
            bool gazeConsented = true,
            bool physiologicalConsented = true)
        {
            if (_state != SessionState.Idle)
            {
                Debug.LogWarning($"[ReadingSessionController] Cannot begin session: state is {_state}.");
                return;
            }

            EnsureDependenciesInjected();

            _conditions = conditions;
            _conditionIndex = 0;
            _activePassage = passage;

            var conditionIds = new List<string>(conditions.Count);
            foreach (var c in conditions) conditionIds.Add(c.ConditionId);

            _activeSession = new ReadingSession(
                participantId, profile, conditionIds,
                ipd, gazeConsented, physiologicalConsented, _appVersion);

            await _repository!.BeginSessionAsync(_activeSession);

            SubscribeEvents();

            _eyeTracker!.StartTracking();
            _bookPresenter!.LoadPassage(passage);

            ApplyCondition(_conditionIndex);
            TransitionTo(SessionState.Active);

            ReadingEventBus.Instance.Publish(new SessionStartedEvent(_activeSession));
            _passageStartTime = DateTime.UtcNow;

            LogState($"Session {_activeSession.SessionId} started for participant {participantId}.");
        }

        /// <summary>Pauses an active session (e.g., participant break).</summary>
        public void PauseSession()
        {
            if (_state != SessionState.Active) return;
            _eyeTracker?.StopTracking();
            TransitionTo(SessionState.Paused);
            LogState("Session paused.");
        }

        /// <summary>Resumes a paused session.</summary>
        public void ResumeSession()
        {
            if (_state != SessionState.Paused) return;
            _eyeTracker?.StartTracking();
            TransitionTo(SessionState.Active);
            LogState("Session resumed.");
        }

        /// <summary>
        /// Advances to the next typography condition.
        /// Records metrics for the current condition before switching.
        /// </summary>
        public async void AdvanceCondition()
        {
            if (_state != SessionState.Active) return;

            await RecordCurrentPassageMetrics();

            _conditionIndex++;
            if (_conditionIndex >= _conditions.Count)
            {
                await EndSession();
                return;
            }

            ApplyCondition(_conditionIndex);
            _passageStartTime = DateTime.UtcNow;
            LogState($"Switched to condition {_conditions[_conditionIndex].ConditionId}.");
        }

        /// <summary>Ends the session, flushes all data, and transitions to Completed.</summary>
        public async System.Threading.Tasks.Task EndSession()
        {
            if (_state == SessionState.Completed) return;

            _eyeTracker?.StopTracking();
            UnsubscribeEvents();

            _activeSession?.Complete();
            if (_activeSession != null)
                ReadingEventBus.Instance.Publish(new SessionEndedEvent(_activeSession));

            await _repository!.EndSessionAsync();
            TransitionTo(SessionState.Completed);
            LogState($"Session {_activeSession?.SessionId} completed.");
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private void ApplyCondition(int index)
        {
            var config = _conditions[index];
            _bookPresenter!.ApplyTypography(config);

            // Stamp the condition ID on the eye tracker context.
            if (_eyeTracker is Services.VarjoEyeTrackingService varjo)
                varjo.SetContext(_activeSession!.SessionId, config.ConditionId);
            else if (_eyeTracker is Services.MockEyeTrackingService mock)
                mock.SetContext(_activeSession!.SessionId, config.ConditionId);
        }

        private async System.Threading.Tasks.Task RecordCurrentPassageMetrics()
        {
            if (_activeSession == null || _activePassage == null) return;

            var config = _conditions[_conditionIndex];

            // Minimal metric stub for Sprint 0 — full metric computation in Sprint 2.
            var metrics = new ReadingMetrics(
                _activeSession.SessionId,
                _activePassage.PassageId,
                config.ConditionId,
                _passageStartTime,
                DateTime.UtcNow)
            {
                PassageWordCount = _activePassage.WordCount,
                FleschKincaidGradeLevel = _activePassage.FleschKincaidGradeLevel
            };

            await _repository!.RecordReadingMetricsAsync(metrics);
            ReadingEventBus.Instance.Publish(new PassageCompletedEvent(metrics));
        }

        private void OnGazeSampled(GazeDataPoint point)
        {
            if (_state != SessionState.Active) return;
            if (_activeSession?.GazeRecordingConsented != true) return;

            // Fire and forget — RecordGazePointAsync is non-blocking.
            _ = _repository!.RecordGazePointAsync(point);
            ReadingEventBus.Instance.Publish(new GazeSampledEvent(point));
        }

        private void SubscribeEvents()
        {
            if (_eyeTracker != null)
                _eyeTracker.GazeSampleReceived += OnGazeSampled;
        }

        private void UnsubscribeEvents()
        {
            if (_eyeTracker != null)
                _eyeTracker.GazeSampleReceived -= OnGazeSampled;
        }

        private void TransitionTo(SessionState next)
        {
            LogState($"{_state} → {next}");
            _state = next;
        }

        private void EnsureDependenciesInjected()
        {
            if (_eyeTracker == null || _repository == null || _bookPresenter == null)
                throw new InvalidOperationException(
                    "[ReadingSessionController] Dependencies not injected. " +
                    "Call Inject() before BeginSession().");
        }

        private void LogState(string message)
        {
            if (_logStateTransitions)
                Debug.Log($"[ReadingSessionController] {message}");
        }
    }
}
