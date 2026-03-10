using System;
using System.Collections.Generic;
using AdapTypeXR.Core.Events;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using UnityEngine;

namespace AdapTypeXR.Presenters
{
    /// <summary>
    /// Controls the 3D book GameObject in the XR scene.
    /// Manages page navigation, text display, and physical book appearance.
    ///
    /// Page discovery: if <see cref="_pageObjects"/> is empty in the inspector,
    /// Awake will auto-discover all child GameObjects tagged "BookPage".
    /// This allows the scene builder to set up the hierarchy without manually
    /// wiring every page reference.
    ///
    /// Page navigation: keyboard (← → arrows) always works in simulation;
    /// XR controller interaction via the optional interactables is additive.
    ///
    /// Design pattern: Composite — the book is a composite of page presenters.
    /// </summary>
    public sealed class BookPresenter : MonoBehaviour, IBookPresenter
    {
        // ── IBookPresenter Events ──────────────────────────────────────────

        /// <inheritdoc />
        public event Action<int>? PageAdvanced;

        /// <inheritdoc />
        public event Action<int>? PageReturned;

        /// <inheritdoc />
        public event Action? BookOpened;

        /// <inheritdoc />
        public event Action? BookClosed;

        // ── Inspector Configuration ────────────────────────────────────────

        [Header("Pages")]
        [Tooltip("Ordered page GameObjects. Leave empty to auto-discover by 'BookPage' tag.")]
        [SerializeField] private List<GameObject> _pageObjects = new();

        [Header("Keyboard Navigation (Simulation)")]
        [SerializeField] private KeyCode _nextPageKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode _prevPageKey = KeyCode.LeftArrow;

        [Header("Book Animation")]
        [Tooltip("Optional animator for open/close and page-turn animations.")]
        [SerializeField] private Animator? _bookAnimator;

        private static readonly int AnimIsOpen = Animator.StringToHash("IsOpen");
        private static readonly int AnimPageTurn = Animator.StringToHash("PageTurn");

        // ── State ──────────────────────────────────────────────────────────

        private ReadingPassage? _activePassage;
        private TypographyConfig? _activeConfig;

        // ── IBookPresenter Properties ──────────────────────────────────────

        /// <inheritdoc />
        public int CurrentPageIndex { get; private set; }

        /// <inheritdoc />
        public int TotalPages => _activePassage?.Pages.Count ?? 0;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_pageObjects.Count == 0)
                AutoDiscoverPages();

            ValidatePageObjects();
        }

        private void Update()
        {
            // Keyboard navigation for simulation and desktop testing.
            if (Input.GetKeyDown(_nextPageKey))
                AdvancePage();
            else if (Input.GetKeyDown(_prevPageKey))
                ReturnPage();
        }

        // ── IBookPresenter Implementation ──────────────────────────────────

        /// <inheritdoc />
        public void LoadPassage(ReadingPassage passage)
        {
            _activePassage = passage;
            CurrentPageIndex = 0;

            for (int i = 0; i < _pageObjects.Count; i++)
            {
                bool hasContent = i < passage.Pages.Count;
                _pageObjects[i].SetActive(hasContent);

                if (hasContent && _activeConfig != null)
                {
                    var renderer = _pageObjects[i].GetComponentInChildren<ITextRenderer>();
                    renderer?.RenderText(passage.Pages[i], _activeConfig);
                }
            }

            ShowPage(0);
            Debug.Log($"[BookPresenter] Loaded passage '{passage.Title}' ({passage.Pages.Count} pages).");
        }

        /// <inheritdoc />
        public void ApplyTypography(TypographyConfig config)
        {
            var previous = _activeConfig;
            _activeConfig = config;

            if (_activePassage == null) return;

            for (int i = 0; i < _pageObjects.Count && i < _activePassage.Pages.Count; i++)
            {
                var renderer = _pageObjects[i].GetComponentInChildren<ITextRenderer>();
                renderer?.RenderText(_activePassage.Pages[i], config);
            }

            if (previous != null)
                ReadingEventBus.Instance.Publish(new ConditionChangedEvent(previous, config));
        }

        /// <inheritdoc />
        public void GoToPage(int pageIndex)
        {
            if (_activePassage == null) return;
            pageIndex = Mathf.Clamp(pageIndex, 0, TotalPages - 1);

            int previous = CurrentPageIndex;
            ShowPage(pageIndex);

            if (pageIndex > previous)
                PageAdvanced?.Invoke(pageIndex);
            else if (pageIndex < previous)
                PageReturned?.Invoke(pageIndex);

            ReadingEventBus.Instance.Publish(new PageTurnedEvent(previous, pageIndex));
        }

        /// <inheritdoc />
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private void AdvancePage()
        {
            if (CurrentPageIndex < TotalPages - 1)
                GoToPage(CurrentPageIndex + 1);
        }

        private void ReturnPage()
        {
            if (CurrentPageIndex > 0)
                GoToPage(CurrentPageIndex - 1);
        }

        private void ShowPage(int index)
        {
            for (int i = 0; i < _pageObjects.Count; i++)
                _pageObjects[i].SetActive(i == index);

            CurrentPageIndex = index;
            _bookAnimator?.SetTrigger(AnimPageTurn);
        }

        /// <summary>
        /// Finds all child GameObjects tagged "BookPage" and adds them to the page list.
        /// Pages are sorted by sibling index (top-down in the hierarchy).
        /// </summary>
        private void AutoDiscoverPages()
        {
            _pageObjects.Clear();
            foreach (Transform child in transform)
            {
                if (child.CompareTag("BookPage"))
                    _pageObjects.Add(child.gameObject);
            }

            if (_pageObjects.Count > 0)
                Debug.Log($"[BookPresenter] Auto-discovered {_pageObjects.Count} pages by 'BookPage' tag.");
            else
                Debug.LogWarning("[BookPresenter] No pages found. Add child objects tagged 'BookPage'.");
        }

        private void ValidatePageObjects()
        {
            for (int i = 0; i < _pageObjects.Count; i++)
            {
                if (_pageObjects[i] == null)
                {
                    Debug.LogError($"[BookPresenter] Page at index {i} is null.");
                    continue;
                }

                if (_pageObjects[i].GetComponentInChildren<ITextRenderer>() == null)
                    Debug.LogWarning($"[BookPresenter] Page '{_pageObjects[i].name}' has no ITextRenderer. " +
                        "Text will not render on this page.");
            }
        }
    }
}
