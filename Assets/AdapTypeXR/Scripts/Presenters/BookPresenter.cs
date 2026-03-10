using System;
using System.Collections.Generic;
using AdapTypeXR.Core.Events;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace AdapTypeXR.Presenters
{
    /// <summary>
    /// Controls the 3D book GameObject in the XR scene.
    /// Manages page navigation, text display, and physical book appearance.
    ///
    /// The book is composed of page GameObjects, each carrying a
    /// <see cref="ITextRenderer"/> component. Pages are swapped by
    /// enabling/disabling GameObjects — no instantiation during reading.
    ///
    /// Design pattern: Composite — the book is a composite of page presenters.
    /// Interaction: delegates to XR Interaction Toolkit grab/poke events.
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

        [Header("Page References")]
        [Tooltip("Ordered list of page GameObjects. Each must have an ITextRenderer component.")]
        [SerializeField] private List<GameObject> _pageObjects = new();

        [Tooltip("Physical next-page button collider (poke or grab interaction).")]
        [SerializeField] private XRSimpleInteractable? _nextPageInteractable;

        [Tooltip("Physical previous-page button collider.")]
        [SerializeField] private XRSimpleInteractable? _prevPageInteractable;

        [Header("Book Animation")]
        [Tooltip("Animator controlling the book open/close and page-turn animations.")]
        [SerializeField] private Animator? _bookAnimator;

        private static readonly int AnimIsOpen = Animator.StringToHash("IsOpen");
        private static readonly int AnimPageTurn = Animator.StringToHash("PageTurn");

        // ── State ──────────────────────────────────────────────────────────

        private ReadingPassage? _activePassage;
        private TypographyConfig? _activeConfig;
        private bool _isOpen;

        // ── IBookPresenter Properties ──────────────────────────────────────

        /// <inheritdoc />
        public int CurrentPageIndex { get; private set; }

        /// <inheritdoc />
        public int TotalPages => _activePassage?.Pages.Count ?? 0;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            ValidatePageObjects();
        }

        private void OnEnable()
        {
            if (_nextPageInteractable != null)
                _nextPageInteractable.selectEntered.AddListener(_ => AdvancePage());

            if (_prevPageInteractable != null)
                _prevPageInteractable.selectEntered.AddListener(_ => ReturnPage());
        }

        private void OnDisable()
        {
            if (_nextPageInteractable != null)
                _nextPageInteractable.selectEntered.RemoveAllListeners();

            if (_prevPageInteractable != null)
                _prevPageInteractable.selectEntered.RemoveAllListeners();
        }

        // ── IBookPresenter Implementation ──────────────────────────────────

        /// <inheritdoc />
        public void LoadPassage(ReadingPassage passage)
        {
            _activePassage = passage;
            CurrentPageIndex = 0;

            // Populate each page GameObject with its text.
            for (int i = 0; i < _pageObjects.Count; i++)
            {
                bool hasContent = i < passage.Pages.Count;
                _pageObjects[i].SetActive(hasContent);

                if (hasContent && _activeConfig != null)
                {
                    var renderer = _pageObjects[i].GetComponent<ITextRenderer>();
                    renderer?.RenderText(passage.Pages[i], _activeConfig);
                }
            }

            ShowPage(0);
            Debug.Log($"[BookPresenter] Loaded passage '{passage.Title}' ({passage.Pages.Count} pages).");
        }

        /// <inheritdoc />
        public void ApplyTypography(TypographyConfig config)
        {
            _activeConfig = config;

            if (_activePassage == null) return;

            for (int i = 0; i < _pageObjects.Count && i < _activePassage.Pages.Count; i++)
            {
                var renderer = _pageObjects[i].GetComponent<ITextRenderer>();
                if (renderer != null)
                    renderer.RenderText(_activePassage.Pages[i], config);
            }

            ReadingEventBus.Instance.Publish(new ConditionChangedEvent(
                _activeConfig ?? config, config));
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

        private void OpenBook()
        {
            _isOpen = true;
            _bookAnimator?.SetBool(AnimIsOpen, true);
            BookOpened?.Invoke();
        }

        private void CloseBook()
        {
            _isOpen = false;
            _bookAnimator?.SetBool(AnimIsOpen, false);
            BookClosed?.Invoke();
        }

        private void ValidatePageObjects()
        {
            for (int i = 0; i < _pageObjects.Count; i++)
            {
                if (_pageObjects[i] == null)
                {
                    Debug.LogError($"[BookPresenter] Page object at index {i} is null.");
                    continue;
                }

                var renderer = _pageObjects[i].GetComponent<ITextRenderer>();
                if (renderer == null)
                    Debug.LogWarning($"[BookPresenter] Page object '{_pageObjects[i].name}' " +
                        "has no ITextRenderer component. Text will not be rendered on this page.");
            }
        }
    }
}
