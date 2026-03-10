using System;
using System.Collections.Generic;
using AdapTypeXR.Core.Interfaces;
using AdapTypeXR.Core.Models;
using TMPro;
using UnityEngine;

namespace AdapTypeXR.Typography
{
    /// <summary>
    /// Renders text on a TextMeshPro component according to a <see cref="TypographyConfig"/>.
    /// Implements <see cref="ITextRenderer"/> so the BookPresenter and other scene components
    /// never depend on TMP directly.
    ///
    /// Bionic reading text transformation is handled here.
    /// Animation delegation is handled by <see cref="TypographyAnimator"/>.
    ///
    /// Design pattern: Façade — presents a simple rendering API over TMP's complex interface.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextRendererController : MonoBehaviour, ITextRenderer
    {
        // ── Dependencies ───────────────────────────────────────────────────

        private TextMeshProUGUI _tmp = null!;

        [Tooltip("Reference to the TypographyAnimator on this GameObject.")]
        [SerializeField] private TypographyAnimator? _animator;

        // ── State ──────────────────────────────────────────────────────────

        private string _currentText = string.Empty;
        private TypographyConfig? _currentConfig;
        private readonly List<string> _words = new();

        // ── ITextRenderer ──────────────────────────────────────────────────

        /// <inheritdoc />
        public bool IsAnimating => _animator != null && _animator.IsRunning;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _tmp = GetComponent<TextMeshProUGUI>();
        }

        // ── ITextRenderer Implementation ───────────────────────────────────

        /// <inheritdoc />
        public void RenderText(string text, TypographyConfig config)
        {
            _currentText = text;
            _currentConfig = config;
            _words.Clear();
            _words.AddRange(text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            ApplyTmpSettings(config);

            _tmp.text = config.EnableBionicReading
                ? BuildBionicReadingMarkup(text)
                : text;

            if (_animator != null)
                _animator.Activate(text, config, this);
        }

        /// <inheritdoc />
        public void ApplyConfig(TypographyConfig config)
        {
            _currentConfig = config;
            ApplyTmpSettings(config);

            if (_currentText.Length > 0)
            {
                _tmp.text = config.EnableBionicReading
                    ? BuildBionicReadingMarkup(_currentText)
                    : _currentText;
            }
        }

        /// <inheritdoc />
        public void HighlightWord(int wordIndex)
        {
            if (_currentConfig == null || wordIndex < 0 || wordIndex >= _words.Count) return;

            // Build TMP rich text with a coloured span around the target word.
            var sb = new System.Text.StringBuilder();
            var highlightHex = ColorUtility.ToHtmlStringRGBA(_currentConfig.HighlightColour);

            for (int i = 0; i < _words.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                if (i == wordIndex)
                    sb.Append($"<mark=#{highlightHex}>{_words[i]}</mark>");
                else
                    sb.Append(_words[i]);
            }

            _tmp.text = sb.ToString();
        }

        /// <inheritdoc />
        public void ClearHighlight()
        {
            if (_currentConfig == null) return;
            _tmp.text = _currentConfig.EnableBionicReading
                ? BuildBionicReadingMarkup(_currentText)
                : _currentText;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _currentText = string.Empty;
            _words.Clear();
            _tmp.text = string.Empty;
            _animator?.Deactivate();
        }

        // ── Private Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Translates a <see cref="TypographyConfig"/> into TMP property assignments.
        /// </summary>
        private void ApplyTmpSettings(TypographyConfig config)
        {
            // Load font asset from Resources if the path is set.
            if (!string.IsNullOrEmpty(config.FontAssetPath))
            {
                var fontAsset = Resources.Load<TMP_FontAsset>(config.FontAssetPath);
                if (fontAsset != null)
                    _tmp.font = fontAsset;
                else
                    Debug.LogWarning($"[TextRendererController] Font asset not found at: {config.FontAssetPath}");
            }

            _tmp.fontSize = config.FontSize;
            _tmp.lineSpacing = (config.LineSpacing - 1f) * 100f; // TMP uses % relative to font size
            _tmp.characterSpacing = config.LetterSpacing * 100f;
            _tmp.wordSpacing = config.WordSpacing * 100f;
            _tmp.paragraphSpacing = (config.ParagraphSpacing - 1f) * 100f;
            _tmp.color = config.TextColour;
        }

        /// <summary>
        /// Transforms plain text into TMP rich text with bold emphasis on
        /// the first half of each word (Bionic Reading technique).
        /// </summary>
        private static string BuildBionicReadingMarkup(string text)
        {
            var words = text.Split(' ');
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                var word = words[i];
                if (word.Length <= 1)
                {
                    sb.Append(word);
                    continue;
                }

                // Bold the first ⌈n/2⌉ characters, leave the rest normal weight.
                int boldLength = Mathf.CeilToInt(word.Length / 2f);
                sb.Append("<b>");
                sb.Append(word[..boldLength]);
                sb.Append("</b>");
                sb.Append(word[boldLength..]);
            }

            return sb.ToString();
        }
    }
}
