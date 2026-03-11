#nullable enable
using System;
using System.Collections.Generic;
using AdapTypeXR.Core.Events;
using AdapTypeXR.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AdapTypeXR.UI
{
    /// <summary>
    /// 3D world-space panel that presents comprehension questions after a reading passage.
    ///
    /// Workflow:
    /// 1. Researcher triggers questions via F3 key (or the panel appears automatically
    ///    when a passage is completed, if wired via events).
    /// 2. Questions are shown one at a time with the question text.
    /// 3. The participant provides a verbal response; the researcher clicks "Volgende"
    ///    to advance. Response timing is recorded automatically.
    /// 4. On completion, a <see cref="ComprehensionCompletedEvent"/> is published.
    ///
    /// The panel does not record free-text input from the participant directly --
    /// in XR, responses are typically verbal and transcribed offline. The panel
    /// records timing and question order for the data pipeline.
    /// </summary>
    public sealed class ComprehensionQuestionPanel : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private Key _toggleKey = Key.F3;

        [Header("Panel Dimensions (world units)")]
        [SerializeField] private float _panelWidth = 0.45f;
        [SerializeField] private float _panelHeight = 0.35f;

        // ── Events ──────────────────────────────────────────────────────────

        /// <summary>Raised when a question is answered (timed). Carries the response.</summary>
        public event Action<ComprehensionResponse>? ResponseRecorded;

        /// <summary>Raised when all questions for the passage are complete.</summary>
        public event Action? AllQuestionsCompleted;

        // ── Runtime State ──────────────────────────────────────────────────

        private GameObject? _panelRoot;
        private TextMeshProUGUI? _questionText;
        private TextMeshProUGUI? _progressText;
        private TextMeshProUGUI? _titleText;
        private Button? _nextButton;
        private TextMeshProUGUI? _nextButtonLabel;

        private ReadingPassage? _passage;
        private string _sessionId = string.Empty;
        private string _conditionId = string.Empty;
        private int _currentIndex;
        private DateTime _questionStartTime;
        private readonly List<ComprehensionResponse> _responses = new();

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            BuildWorldCanvas();
            Hide();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame)
                Toggle();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Begins presenting comprehension questions for the given passage.
        /// </summary>
        public void ShowQuestions(ReadingPassage passage, string sessionId, string conditionId)
        {
            _passage = passage;
            _sessionId = sessionId;
            _conditionId = conditionId;
            _currentIndex = 0;
            _responses.Clear();

            if (passage.Questions.Count == 0)
            {
                Debug.LogWarning("[ComprehensionQuestionPanel] No questions for this passage.");
                return;
            }

            Show();
            DisplayCurrentQuestion();
        }

        /// <summary>Shows or hides the panel.</summary>
        public void Toggle()
        {
            if (_panelRoot == null) return;
            if (_panelRoot.activeSelf) Hide(); else Show();
        }

        /// <summary>Returns all recorded responses from the current question set.</summary>
        public IReadOnlyList<ComprehensionResponse> GetResponses() => _responses;

        // ── Private ────────────────────────────────────────────────────────

        private void Show() => _panelRoot?.SetActive(true);
        private void Hide() => _panelRoot?.SetActive(false);

        private void DisplayCurrentQuestion()
        {
            if (_passage == null || _currentIndex >= _passage.Questions.Count) return;

            var q = _passage.Questions[_currentIndex];

            if (_titleText != null)
                _titleText.text = _passage.Title;

            if (_questionText != null)
                _questionText.text = q.QuestionText;

            if (_progressText != null)
                _progressText.text = $"Vraag {_currentIndex + 1} / {_passage.Questions.Count}";

            bool isLast = _currentIndex == _passage.Questions.Count - 1;
            if (_nextButtonLabel != null)
                _nextButtonLabel.text = isLast ? "Afronden" : "Volgende";

            _questionStartTime = DateTime.UtcNow;
        }

        private void OnNextClicked()
        {
            if (_passage == null || _currentIndex >= _passage.Questions.Count) return;

            var q = _passage.Questions[_currentIndex];
            float elapsed = (float)(DateTime.UtcNow - _questionStartTime).TotalSeconds;

            var response = new ComprehensionResponse(
                sessionId: _sessionId,
                passageId: _passage.PassageId,
                questionId: q.QuestionId,
                conditionId: _conditionId,
                responseText: string.Empty, // verbal response transcribed offline
                submittedAt: DateTime.UtcNow,
                responseTimeSeconds: elapsed);

            _responses.Add(response);
            ResponseRecorded?.Invoke(response);

            _currentIndex++;
            if (_currentIndex < _passage.Questions.Count)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                // All done
                if (_questionText != null)
                    _questionText.text = "Alle vragen zijn beantwoord.";
                if (_progressText != null)
                    _progressText.text = "Klaar";
                if (_nextButton != null)
                    _nextButton.interactable = false;

                AllQuestionsCompleted?.Invoke();
                ReadingEventBus.Instance.Publish(
                    new ComprehensionCompletedEvent(_passage.PassageId, _responses.Count));
            }
        }

        // ── World Canvas Builder ────────────────────────────────────────────

        private void BuildWorldCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 26;

            var canvasRt = GetComponent<RectTransform>();
            float pixelsPerUnit = 1000f;
            canvasRt.sizeDelta = new Vector2(_panelWidth * pixelsPerUnit, _panelHeight * pixelsPerUnit);
            transform.localScale = Vector3.one / pixelsPerUnit;

            gameObject.AddComponent<GraphicRaycaster>();

            _panelRoot = new GameObject("Panel");
            _panelRoot.transform.SetParent(transform, false);

            var panelRt = _panelRoot.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var panelBg = _panelRoot.AddComponent<Image>();
            panelBg.color = new Color(0.06f, 0.06f, 0.08f, 0.95f);

            BuildContent();
        }

        private void BuildContent()
        {
            if (_panelRoot == null) return;

            // Title (passage name)
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panelRoot.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, -42f);
            titleRt.offsetMax = new Vector2(-16f, -10f);
            _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.text = "Begripsvragen";
            _titleText.fontSize = 18f;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = Color.white;

            // Separator
            var sep = new GameObject("Sep");
            sep.transform.SetParent(_panelRoot.transform, false);
            var sepRt = sep.AddComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 1f);
            sepRt.anchorMax = new Vector2(1f, 1f);
            sepRt.offsetMin = new Vector2(12f, -46f);
            sepRt.offsetMax = new Vector2(-12f, -45f);
            sep.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            // Progress label
            var progGo = new GameObject("Progress");
            progGo.transform.SetParent(_panelRoot.transform, false);
            var progRt = progGo.AddComponent<RectTransform>();
            progRt.anchorMin = new Vector2(0f, 1f);
            progRt.anchorMax = new Vector2(1f, 1f);
            progRt.offsetMin = new Vector2(16f, -66f);
            progRt.offsetMax = new Vector2(-16f, -48f);
            _progressText = progGo.AddComponent<TextMeshProUGUI>();
            _progressText.text = "Vraag — / —";
            _progressText.fontSize = 13f;
            _progressText.color = new Color(0.55f, 0.78f, 1f);

            // Question text area
            var qGo = new GameObject("QuestionText");
            qGo.transform.SetParent(_panelRoot.transform, false);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0f, 0f);
            qRt.anchorMax = new Vector2(1f, 1f);
            qRt.offsetMin = new Vector2(16f, 60f);
            qRt.offsetMax = new Vector2(-16f, -70f);
            _questionText = qGo.AddComponent<TextMeshProUGUI>();
            _questionText.text = "";
            _questionText.fontSize = 17f;
            _questionText.color = new Color(0.92f, 0.92f, 0.94f);
            _questionText.enableWordWrapping = true;
            _questionText.alignment = TextAlignmentOptions.TopLeft;

            // Next button
            var btnGo = new GameObject("NextButton");
            btnGo.transform.SetParent(_panelRoot.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.3f, 0f);
            btnRt.anchorMax = new Vector2(0.7f, 0f);
            btnRt.offsetMin = new Vector2(0f, 14f);
            btnRt.offsetMax = new Vector2(0f, 50f);

            var btnBg = btnGo.AddComponent<Image>();
            btnBg.color = new Color(0.15f, 0.38f, 0.18f, 0.95f);

            _nextButton = btnGo.AddComponent<Button>();
            _nextButton.targetGraphic = btnBg;
            var cols = _nextButton.colors;
            cols.normalColor = new Color(0.15f, 0.38f, 0.18f, 0.95f);
            cols.highlightedColor = new Color(0.22f, 0.50f, 0.26f, 1f);
            cols.pressedColor = new Color(0.10f, 0.28f, 0.12f, 1f);
            _nextButton.colors = cols;
            _nextButton.onClick.AddListener(OnNextClicked);

            var btnLabelGo = new GameObject("Label");
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            var btnLabelRt = btnLabelGo.AddComponent<RectTransform>();
            btnLabelRt.anchorMin = Vector2.zero;
            btnLabelRt.anchorMax = Vector2.one;
            btnLabelRt.offsetMin = new Vector2(4f, 2f);
            btnLabelRt.offsetMax = new Vector2(-4f, -2f);
            _nextButtonLabel = btnLabelGo.AddComponent<TextMeshProUGUI>();
            _nextButtonLabel.text = "Volgende";
            _nextButtonLabel.fontSize = 16f;
            _nextButtonLabel.color = new Color(0.8f, 1f, 0.8f);
            _nextButtonLabel.alignment = TextAlignmentOptions.Center;

            // Toggle hint
            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(_panelRoot.transform, false);
            var hintRt = hintGo.AddComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.offsetMin = new Vector2(8f, 0f);
            hintRt.offsetMax = new Vector2(-8f, 14f);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "[F3] verberg / toon vragen";
            hintTmp.fontSize = 10f;
            hintTmp.color = new Color(0.32f, 0.32f, 0.36f);
            hintTmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
