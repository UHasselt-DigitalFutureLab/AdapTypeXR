#nullable enable
using System;
using System.Collections.Generic;
using AdapTypeXR.Core;
using AdapTypeXR.Core.Models;
using AdapTypeXR.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AdapTypeXR.UI
{
    /// <summary>
    /// 3D world-space passage selection panel for XR environments.
    ///
    /// Builds a world-space Canvas at runtime, positioned to the left of the book.
    /// Reads passages from <see cref="PassageLibrary.GetAllPassages"/> and loads
    /// the chosen passage into the active <see cref="BookPresenter"/>.
    ///
    /// Toggle: F2 key.
    /// </summary>
    public sealed class PassageSelectorPanel : MonoBehaviour
    {
        [Header("Toggle")]
        [Tooltip("Key that shows/hides the passage selector panel.")]
        [SerializeField] private Key _toggleKey = Key.F2;

        [Header("Panel Dimensions (world units)")]
        [SerializeField] private float _panelWidth = 0.40f;
        [SerializeField] private float _panelHeight = 0.45f;

        // ── Events ──────────────────────────────────────────────────────────

        /// <summary>Raised when the researcher selects a passage.</summary>
        public event Action<ReadingPassage>? PassageSelected;

        // ── Runtime State ──────────────────────────────────────────────────

        private BookPresenter? _bookPresenter;
        private GameObject? _panelRoot;
        private TextMeshProUGUI? _selectedLabel;
        private Transform? _buttonContainer;

        private readonly List<PassageEntry> _entries = new();
        private int _activeIndex = -1;

        private struct PassageEntry
        {
            public ReadingPassage Passage;
            public Image Background;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            BuildWorldCanvas();
        }

        private void Start()
        {
            _bookPresenter = FindFirstObjectByType<BookPresenter>();
            PopulateButtons();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame)
                Toggle();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Shows or hides the passage selector panel.</summary>
        public void Toggle() =>
            _panelRoot?.SetActive(!(_panelRoot.activeSelf));

        // ── World Canvas Builder ────────────────────────────────────────────

        private void BuildWorldCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 24;

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
            panelBg.color = new Color(0.06f, 0.06f, 0.08f, 0.93f);

            BuildHeader();
            BuildScrollArea();
        }

        private void BuildHeader()
        {
            if (_panelRoot == null) return;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panelRoot.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, -48f);
            titleRt.offsetMax = new Vector2(-16f, -10f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Tekst kiezen";
            titleTmp.fontSize = 22f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;

            // Separator
            var sep = new GameObject("Sep");
            sep.transform.SetParent(_panelRoot.transform, false);
            var sepRt = sep.AddComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 1f);
            sepRt.anchorMax = new Vector2(1f, 1f);
            sepRt.offsetMin = new Vector2(12f, -52f);
            sepRt.offsetMax = new Vector2(-12f, -51f);
            sep.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            // Active passage label
            var selGo = new GameObject("ActivePassage");
            selGo.transform.SetParent(_panelRoot.transform, false);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 1f);
            selRt.anchorMax = new Vector2(1f, 1f);
            selRt.offsetMin = new Vector2(16f, -74f);
            selRt.offsetMax = new Vector2(-16f, -54f);
            _selectedLabel = selGo.AddComponent<TextMeshProUGUI>();
            _selectedLabel.text = "\u2014";
            _selectedLabel.fontSize = 15f;
            _selectedLabel.color = new Color(0.78f, 0.88f, 0.55f);
            _selectedLabel.enableWordWrapping = false;
            _selectedLabel.overflowMode = TextOverflowModes.Ellipsis;

            // Toggle hint at bottom
            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(_panelRoot.transform, false);
            var hintRt = hintGo.AddComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.offsetMin = new Vector2(8f, 8f);
            hintRt.offsetMax = new Vector2(-8f, 26f);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "[F2] verberg / toon panel";
            hintTmp.fontSize = 12f;
            hintTmp.color = new Color(0.32f, 0.32f, 0.36f);
            hintTmp.alignment = TextAlignmentOptions.Center;
        }

        private void BuildScrollArea()
        {
            if (_panelRoot == null) return;

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_panelRoot.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(8f, 34f);
            scrollRt.offsetMax = new Vector2(-8f, -78f);

            var maskBg = scrollGo.AddComponent<Image>();
            maskBg.color = new Color(0f, 0f, 0f, 0.01f);
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 22f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 4, 4);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRt;
            _buttonContainer = contentGo.transform;
        }

        // ── Button Population ──────────────────────────────────────────────

        private void PopulateButtons()
        {
            if (_buttonContainer == null) return;

            var passages = PassageLibrary.GetAllPassages();
            foreach (var passage in passages)
                AddPassageButton(passage);
        }

        private void AddPassageButton(ReadingPassage passage)
        {
            if (_buttonContainer == null) return;

            var btnGo = new GameObject(passage.PassageId);
            btnGo.transform.SetParent(_buttonContainer, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 62f);

            var bg = btnGo.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.14f, 0.17f, 0.92f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors;
            cols.normalColor = new Color(0.14f, 0.14f, 0.17f, 0.92f);
            cols.highlightedColor = new Color(0.22f, 0.22f, 0.28f, 1f);
            cols.pressedColor = new Color(0.10f, 0.10f, 0.13f, 1f);
            btn.colors = cols;

            // Title label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.4f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(14f, 0f);
            labelRt.offsetMax = new Vector2(-14f, -4f);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = passage.Title;
            labelTmp.fontSize = 15f;
            labelTmp.color = new Color(0.88f, 0.88f, 0.90f);
            labelTmp.enableWordWrapping = true;
            labelTmp.alignment = TextAlignmentOptions.Left;

            // Metadata line
            var metaGo = new GameObject("Meta");
            metaGo.transform.SetParent(btnGo.transform, false);
            var metaRt = metaGo.AddComponent<RectTransform>();
            metaRt.anchorMin = new Vector2(0f, 0f);
            metaRt.anchorMax = new Vector2(1f, 0.4f);
            metaRt.offsetMin = new Vector2(14f, 4f);
            metaRt.offsetMax = new Vector2(-14f, 0f);
            var metaTmp = metaGo.AddComponent<TextMeshProUGUI>();
            metaTmp.text = $"{passage.WordCount} woorden  |  {passage.Pages.Count} pagina's  |  FK {passage.FleschKincaidGradeLevel:F1}";
            metaTmp.fontSize = 11f;
            metaTmp.color = new Color(0.55f, 0.55f, 0.60f);
            metaTmp.enableWordWrapping = false;
            metaTmp.alignment = TextAlignmentOptions.Left;

            int index = _entries.Count;
            _entries.Add(new PassageEntry { Passage = passage, Background = bg });
            btn.onClick.AddListener(() => Apply(index));
        }

        // ── Selection ──────────────────────────────────────────────────────

        private void Apply(int index)
        {
            if (index < 0 || index >= _entries.Count || _bookPresenter == null) return;

            _activeIndex = index;
            var passage = _entries[index].Passage;
            _bookPresenter.LoadPassage(passage);

            if (_selectedLabel != null)
                _selectedLabel.text = passage.Title;

            RefreshHighlights();
            PassageSelected?.Invoke(passage);
        }

        private void RefreshHighlights()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Background.color = i == _activeIndex
                    ? new Color(0.28f, 0.46f, 0.22f, 0.96f)
                    : new Color(0.14f, 0.14f, 0.17f, 0.92f);
            }
        }
    }
}
