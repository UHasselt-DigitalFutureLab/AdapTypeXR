using System;
using System.Collections;
using System.Collections.Generic;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using UnityEngine;

namespace AdapTypeXR.Typography
{
    /// <summary>
    /// Drives text animations by delegating to the appropriate
    /// <see cref="ITypographyAnimationStrategy"/> for the active <see cref="AnimationMode"/>.
    ///
    /// Design pattern: Strategy — new animation modes are added by implementing
    /// <see cref="ITypographyAnimationStrategy"/>; this class never changes.
    ///
    /// The animator is attached to the same GameObject as <see cref="TextRendererController"/>
    /// and receives callbacks from it when a new text/config is applied.
    /// </summary>
    public sealed class TypographyAnimator : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────

        private ITypographyAnimationStrategy? _activeStrategy;
        private ITextRenderer? _renderer;
        private readonly Dictionary<AnimationMode, ITypographyAnimationStrategy> _strategies = new();

        /// <summary>Whether an animation strategy is currently running.</summary>
        public bool IsRunning => _activeStrategy?.IsRunning ?? false;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            // Register all built-in strategies. New strategies can be registered
            // externally via RegisterStrategy without modifying this class.
            RegisterStrategy(new WordByWordHighlightStrategy());
            RegisterStrategy(new RsvpStrategy());
        }

        private void OnDestroy()
        {
            _activeStrategy?.Reset();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Registers a custom animation strategy. If a strategy for the same mode
        /// already exists, it is replaced (Open/Closed via extension without modification).
        /// </summary>
        public void RegisterStrategy(ITypographyAnimationStrategy strategy)
        {
            // Parse mode from strategy name — strategies self-declare their mode via ModeName.
            // For type safety, strategies also expose their AnimationMode via a cast check.
            if (strategy is IAnimationModeProvider modeProvider)
                _strategies[modeProvider.AnimationMode] = strategy;
            else
                Debug.LogWarning($"[TypographyAnimator] Strategy '{strategy.ModeName}' does not " +
                    "implement IAnimationModeProvider and cannot be registered by mode.");
        }

        /// <summary>
        /// Called by <see cref="TextRendererController"/> when new text/config is applied.
        /// Selects and starts the appropriate strategy.
        /// </summary>
        public void Activate(string text, TypographyConfig config, ITextRenderer renderer)
        {
            _renderer = renderer;
            _activeStrategy?.Reset();

            if (config.Animation == AnimationMode.None) return;

            if (!_strategies.TryGetValue(config.Animation, out var strategy))
            {
                Debug.LogWarning($"[TypographyAnimator] No strategy registered for {config.Animation}.");
                return;
            }

            _activeStrategy = strategy;
            _activeStrategy.WordAdvanced += OnWordAdvanced;
            _activeStrategy.AnimationCompleted += OnAnimationCompleted;
            _activeStrategy.Initialise(text, config);
            _activeStrategy.Start();
        }

        /// <summary>Stops the current animation and clears event subscriptions.</summary>
        public void Deactivate()
        {
            if (_activeStrategy == null) return;
            _activeStrategy.WordAdvanced -= OnWordAdvanced;
            _activeStrategy.AnimationCompleted -= OnAnimationCompleted;
            _activeStrategy.Reset();
            _activeStrategy = null;
        }

        // ── Private ────────────────────────────────────────────────────────

        private void OnWordAdvanced(int wordIndex)
        {
            _renderer?.HighlightWord(wordIndex);
        }

        private void OnAnimationCompleted()
        {
            _renderer?.ClearHighlight();
        }
    }

    // ── Supporting Interface ───────────────────────────────────────────────────

    /// <summary>
    /// Allows a strategy to declare which <see cref="AnimationMode"/> it handles,
    /// enabling type-safe registration in <see cref="TypographyAnimator"/>.
    /// </summary>
    public interface IAnimationModeProvider
    {
        AnimationMode AnimationMode { get; }
    }

    // ── Built-in Strategies ────────────────────────────────────────────────────

    /// <summary>
    /// Word-by-word highlight strategy.
    /// Full text is visible; a highlight marker progresses word by word
    /// at the configured WPM rate, preserving re-reading ability.
    /// </summary>
    public sealed class WordByWordHighlightStrategy : MonoBehaviour,
        ITypographyAnimationStrategy, IAnimationModeProvider
    {
        public string ModeName => "WordByWordHighlight";
        public AnimationMode AnimationMode => AnimationMode.WordByWordHighlight;

        public event Action<int>? WordAdvanced;
        public event Action? AnimationCompleted;
        public bool IsRunning { get; private set; }

        private string[] _words = Array.Empty<string>();
        private float _secondsPerWord;
        private int _currentWordIndex;
        private Coroutine? _coroutine;

        public void Initialise(string text, TypographyConfig config)
        {
            _words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _secondsPerWord = 60f / Mathf.Max(1f, config.WordsPerMinute);
            _currentWordIndex = 0;
        }

        public void Start()
        {
            IsRunning = true;
            _coroutine = StartCoroutine(HighlightLoop());
        }

        public void Pause()
        {
            IsRunning = false;
            if (_coroutine != null) StopCoroutine(_coroutine);
        }

        public void Reset()
        {
            Pause();
            _currentWordIndex = 0;
        }

        private IEnumerator HighlightLoop()
        {
            while (_currentWordIndex < _words.Length)
            {
                WordAdvanced?.Invoke(_currentWordIndex);
                yield return new WaitForSeconds(_secondsPerWord);
                _currentWordIndex++;
            }
            IsRunning = false;
            AnimationCompleted?.Invoke();
        }
    }

    /// <summary>
    /// RSVP (Rapid Serial Visual Presentation) strategy.
    /// Displays one word at a time at the centre of the text area.
    /// Eliminates saccade requirements; rate controlled by WPM.
    /// </summary>
    public sealed class RsvpStrategy : MonoBehaviour,
        ITypographyAnimationStrategy, IAnimationModeProvider
    {
        public string ModeName => "RSVP";
        public AnimationMode AnimationMode => AnimationMode.RSVP;

        public event Action<int>? WordAdvanced;
        public event Action? AnimationCompleted;
        public bool IsRunning { get; private set; }

        private string[] _words = Array.Empty<string>();
        private float _secondsPerWord;
        private int _currentWordIndex;
        private Coroutine? _coroutine;

        public void Initialise(string text, TypographyConfig config)
        {
            _words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _secondsPerWord = 60f / Mathf.Max(1f, config.WordsPerMinute);
            _currentWordIndex = 0;
        }

        public void Start()
        {
            IsRunning = true;
            _coroutine = StartCoroutine(RsvpLoop());
        }

        public void Pause()
        {
            IsRunning = false;
            if (_coroutine != null) StopCoroutine(_coroutine);
        }

        public void Reset()
        {
            Pause();
            _currentWordIndex = 0;
        }

        private IEnumerator RsvpLoop()
        {
            while (_currentWordIndex < _words.Length)
            {
                // WordAdvanced here signals the renderer to show only this word.
                // The TextRendererController handles RSVP display mode separately.
                WordAdvanced?.Invoke(_currentWordIndex);
                yield return new WaitForSeconds(_secondsPerWord);
                _currentWordIndex++;
            }
            IsRunning = false;
            AnimationCompleted?.Invoke();
        }
    }
}
