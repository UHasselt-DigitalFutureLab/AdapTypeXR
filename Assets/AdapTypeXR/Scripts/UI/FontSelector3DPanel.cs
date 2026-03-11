#nullable enable
using System.Collections.Generic;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using AdapTypeXR.Presenters;
using AdapTypeXR.Typography;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AdapTypeXR.UI
{
    /// <summary>
    /// 3D world-space font selection panel for XR environments.
    ///
    /// Builds a world-space Canvas at runtime, positioned to the right of the book.
    /// Reads fonts from <see cref="FontProfileFactory.BuildFontSelectorCatalogue"/>
    /// and applies the chosen <see cref="TypographyConfig"/> to the active
    /// <see cref="IBookPresenter"/>.
    ///
    /// Toggle: F1 key (override via inspector).
    ///
    /// Unlike <see cref="FontSelectorPanel"/> (screen-space overlay), this panel
    /// exists in world space and is readable in XR headsets.
    ///
    /// Collaboration: READSEARCH (Ann Bessemans, PXL-MAD / UHasselt)
    ///             x  Digital Future Lab (UHasselt)
    /// </summary>
    public sealed class FontSelector3DPanel : MonoBehaviour
    {
        [Header("Toggle")]
        [Tooltip("Key that shows/hides the font selector panel.")]
        [SerializeField] private Key _toggleKey = Key.F1;

        [Header("Panel Dimensions (world units)")]
        [Tooltip("Width of the panel in world units.")]
        [SerializeField] private float _panelWidth = 0.40f;

        [Tooltip("Height of the panel in world units.")]
        [SerializeField] private float _panelHeight = 0.55f;

        // ── Runtime State ──────────────────────────────────────────────────

        private IBookPresenter? _bookPresenter;
        private GameObject? _panelRoot;
        private TextMeshProUGUI? _selectedLabel;
        private Transform? _buttonContainer;

        private readonly List<FontEntry> _entries = new();
        private int _activeIndex = -1;

        private struct FontEntry
        {
            public TypographyConfig Config;
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
            if (_entries.Count > 0) Apply(0);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame)
                Toggle();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Shows or hides the font selector panel.</summary>
        public void Toggle() =>
            _panelRoot?.SetActive(!(_panelRoot.activeSelf));

        // ── World Canvas Builder ────────────────────────────────────────────

        private void BuildWorldCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 25;

            // Scale the canvas so 1 UI unit = 1 mm in world space.
            // The RectTransform dimensions are in "UI pixels" — we scale down
            // so the panel has a sensible physical size.
            var canvasRt = GetComponent<RectTransform>();
            float pixelsPerUnit = 1000f;
            canvasRt.sizeDelta = new Vector2(_panelWidth * pixelsPerUnit, _panelHeight * pixelsPerUnit);
            transform.localScale = Vector3.one / pixelsPerUnit;

            gameObject.AddComponent<GraphicRaycaster>();

            // ── Panel root (fills the canvas) ──────────────────────────────
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

            // ── Title ──────────────────────────────────────────────────────
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panelRoot.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, -48f);
            titleRt.offsetMax = new Vector2(-16f, -10f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Lettertype kiezen";
            titleTmp.fontSize = 22f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;

            // ── Hairline separator ─────────────────────────────────────────
            var sep = new GameObject("Sep");
            sep.transform.SetParent(_panelRoot.transform, false);
            var sepRt = sep.AddComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 1f);
            sepRt.anchorMax = new Vector2(1f, 1f);
            sepRt.offsetMin = new Vector2(12f, -52f);
            sepRt.offsetMax = new Vector2(-12f, -51f);
            sep.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            // ── Active font label ──────────────────────────────────────────
            var selGo = new GameObject("ActiveFont");
            selGo.transform.SetParent(_panelRoot.transform, false);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 1f);
            selRt.anchorMax = new Vector2(1f, 1f);
            selRt.offsetMin = new Vector2(16f, -74f);
            selRt.offsetMax = new Vector2(-16f, -54f);
            _selectedLabel = selGo.AddComponent<TextMeshProUGUI>();
            _selectedLabel.text = "\u2014";
            _selectedLabel.fontSize = 15f;
            _selectedLabel.color = new Color(0.55f, 0.78f, 1f);
            _selectedLabel.enableWordWrapping = false;
            _selectedLabel.overflowMode = TextOverflowModes.Ellipsis;

            // ── Credit line at bottom ──────────────────────────────────────
            var creditGo = new GameObject("Credit");
            creditGo.transform.SetParent(_panelRoot.transform, false);
            var creditRt = creditGo.AddComponent<RectTransform>();
            creditRt.anchorMin = new Vector2(0f, 0f);
            creditRt.anchorMax = new Vector2(1f, 0f);
            creditRt.offsetMin = new Vector2(8f, 28f);
            creditRt.offsetMax = new Vector2(-8f, 46f);
            var creditTmp = creditGo.AddComponent<TextMeshProUGUI>();
            creditTmp.text = "READSEARCH  x  Digital Future Lab";
            creditTmp.fontSize = 12f;
            creditTmp.color = new Color(0.42f, 0.42f, 0.47f);
            creditTmp.alignment = TextAlignmentOptions.Center;

            // ── Toggle hint ────────────────────────────────────────────────
            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(_panelRoot.transform, false);
            var hintRt = hintGo.AddComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.offsetMin = new Vector2(8f, 8f);
            hintRt.offsetMax = new Vector2(-8f, 26f);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "[F1] verberg / toon panel";
            hintTmp.fontSize = 12f;
            hintTmp.color = new Color(0.32f, 0.32f, 0.36f);
            hintTmp.alignment = TextAlignmentOptions.Center;
        }

        private void BuildScrollArea()
        {
            if (_panelRoot == null) return;

            // ── Scroll root ────────────────────────────────────────────────
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_panelRoot.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(8f, 54f);
            scrollRt.offsetMax = new Vector2(-8f, -78f);

            // Invisible background required by Mask.
            var maskBg = scrollGo.AddComponent<Image>();
            maskBg.color = new Color(0f, 0f, 0f, 0.01f);
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 22f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // ── Content container ──────────────────────────────────────────
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 4, 4);
            layout.childControlHeight = false;
            layout.childControlWidth  = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth  = true;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRt;
            _buttonContainer = contentGo.transform;
        }

        // ── Button Population ──────────────────────────────────────────────

        private void PopulateButtons()
        {
            if (_buttonContainer == null) return;

            var configs = FontProfileFactory.BuildFontSelectorCatalogue();
            foreach (var cfg in configs)
                AddFontButton(cfg);
        }

        private void AddFontButton(TypographyConfig config)
        {
            if (_buttonContainer == null) return;

            var btnGo = new GameObject(config.ConditionId);
            btnGo.transform.SetParent(_buttonContainer, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 52f);

            var bg = btnGo.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.14f, 0.17f, 0.92f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            var cols = btn.colors;
            cols.normalColor      = new Color(0.14f, 0.14f, 0.17f, 0.92f);
            cols.highlightedColor = new Color(0.22f, 0.22f, 0.28f, 1f);
            cols.pressedColor     = new Color(0.10f, 0.10f, 0.13f, 1f);
            btn.colors = cols;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(14f, 4f);
            labelRt.offsetMax = new Vector2(-14f, -4f);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = config.DisplayName;
            labelTmp.fontSize = 16f;
            labelTmp.color = new Color(0.88f, 0.88f, 0.90f);
            labelTmp.enableWordWrapping = true;
            labelTmp.alignment = TextAlignmentOptions.Left;

            int index = _entries.Count;
            _entries.Add(new FontEntry { Config = config, Background = bg });
            btn.onClick.AddListener(() => Apply(index));
        }

        // ── Selection ──────────────────────────────────────────────────────

        private void Apply(int index)
        {
            if (index < 0 || index >= _entries.Count || _bookPresenter == null) return;

            _activeIndex = index;
            _bookPresenter.ApplyTypography(_entries[index].Config);

            if (_selectedLabel != null)
                _selectedLabel.text = _entries[index].Config.DisplayName;

            RefreshHighlights();
        }

        private void RefreshHighlights()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Background.color = i == _activeIndex
                    ? new Color(0.18f, 0.36f, 0.62f, 0.96f)
                    : new Color(0.14f, 0.14f, 0.17f, 0.92f);
            }
        }
    }
}
