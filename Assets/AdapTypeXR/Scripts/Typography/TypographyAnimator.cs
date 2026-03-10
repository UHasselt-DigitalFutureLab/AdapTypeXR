#nullable enable
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
    /// Strategies are plain C# classes. This MonoBehaviour acts as the coroutine
    /// host, calling StartCoroutine/StopCoroutine on their behalf.
    /// </summary>
    public sealed class TypographyAnimator : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────

        private ITypographyAnimationStrategy? _activeStrategy;
        private Coroutine? _activeCoroutine;
        private ITextRenderer? _renderer;
        private readonly Dictionary<AnimationMode, ITypographyAnimationStrategy> _strategies = new();

        /// <summary>Whether an animation strategy is currently running.</summary>
        public bool IsRunning => _activeStrategy?.IsRunning ?? false;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            // Register built-in strategies. These are plain C# classes — NOT MonoBehaviours.
            // New strategies can be registered externally without modifying this class (OCP).
            RegisterStrategy(new WordByWordHighlightStrategy());
            RegisterStrategy(new RsvpStrategy());
        }

        private void OnDestroy()
        {
            Deactivate();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Registers a strategy for the given animation mode.
        /// Replaces any existing strategy for that mode.
        /// </summary>
        public void RegisterStrategy(ITypographyAnimationStrategy strategy)
        {
            _strategies[strategy.AnimationMode] = strategy;
        }

        /// <summary>
        /// Called by <see cref="TextRendererController"/> when new text/config is applied.
        /// Selects and starts the appropriate strategy as a coroutine on this MonoBehaviour.
        /// </summary>
        public void Activate(string text, TypographyConfig config, ITextRenderer renderer)
        {
            Deactivate();
            _renderer = renderer;

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
            _activeCoroutine = StartCoroutine(_activeStrategy.CreateRoutine());
        }

        /// <summary>Stops the current animation and clears event subscriptions.</summary>
        public void Deactivate()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            if (_activeStrategy != null)
            {
                _activeStrategy.Stop();
                _activeStrategy.WordAdvanced -= OnWordAdvanced;
                _activeStrategy.AnimationCompleted -= OnAnimationCompleted;
                _activeStrategy = null;
            }
        }

        // ── Private ────────────────────────────────────────────────────────

        private void OnWordAdvanced(int wordIndex) => _renderer?.HighlightWord(wordIndex);
        private void OnAnimationCompleted() => _renderer?.ClearHighlight();
    }

    // ── Built-in Strategies (plain C# classes) ────────────────────────────────

    /// <summary>
    /// Word-by-word highlight strategy.
    /// Full text remains visible; a highlight progresses word by word at the
    /// configured WPM rate, preserving context and re-reading ability.
    /// Positive evidence for ADHD readers (reduces lost-place errors).
    /// </summary>
    public sealed class WordByWordHighlightStrategy : ITypographyAnimationStrategy
    {
        public AnimationMode AnimationMode => AnimationMode.WordByWordHighlight;
        public string ModeName => "WordByWordHighlight";

        public event Action<int>? WordAdvanced;
        public event Action? AnimationCompleted;
        public bool IsRunning { get; private set; }

        private string[] _words = Array.Empty<string>();
        private float _secondsPerWord;

        public void Initialise(string text, TypographyConfig config)
        {
            _words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _secondsPerWord = 60f / Mathf.Max(1f, config.WordsPerMinute);
            IsRunning = false;
        }

        public IEnumerator CreateRoutine()
        {
            IsRunning = true;
            for (int i = 0; i < _words.Length && IsRunning; i++)
            {
                WordAdvanced?.Invoke(i);
                yield return new WaitForSeconds(_secondsPerWord);
            }
            IsRunning = false;
            AnimationCompleted?.Invoke();
        }

        public void Stop() => IsRunning = false;
    }

    /// <summary>
    /// Rapid Serial Visual Presentation (RSVP) strategy.
    /// Signals the renderer to display one word at a time at a fixed position.
    /// Eliminates the need for saccades; rate controlled by WPM.
    /// Note: prevents re-reading — comprehension impact varies by profile.
    /// </summary>
    public sealed class RsvpStrategy : ITypographyAnimationStrategy
    {
        public AnimationMode AnimationMode => AnimationMode.RSVP;
        public string ModeName => "RSVP";

        public event Action<int>? WordAdvanced;
        public event Action? AnimationCompleted;
        public bool IsRunning { get; private set; }

        private string[] _words = Array.Empty<string>();
        private float _secondsPerWord;

        public void Initialise(string text, TypographyConfig config)
        {
            _words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _secondsPerWord = 60f / Mathf.Max(1f, config.WordsPerMinute);
            IsRunning = false;
        }

        public IEnumerator CreateRoutine()
        {
            IsRunning = true;
            for (int i = 0; i < _words.Length && IsRunning; i++)
            {
                // WordAdvanced signals the renderer to display only this word (RSVP mode).
                WordAdvanced?.Invoke(i);
                yield return new WaitForSeconds(_secondsPerWord);
            }
            IsRunning = false;
            AnimationCompleted?.Invoke();
        }

        public void Stop() => IsRunning = false;
    }
}
