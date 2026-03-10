#nullable enable
using AdapTypeXR.Core.Models;

namespace AdapTypeXR.Core.Interfaces
{
    /// <summary>
    /// Renders text content onto a target surface with a given typography config.
    /// Decouples text display from the book and scene structure, allowing
    /// the same renderer to be used on book pages, UI overlays, or study prompts.
    /// </summary>
    public interface ITextRenderer
    {
        /// <summary>Whether a text animation is currently in progress.</summary>
        bool IsAnimating { get; }

        /// <summary>
        /// Renders a full text string with the provided typography configuration.
        /// Replaces any previously rendered content on this renderer.
        /// </summary>
        /// <param name="text">The text content to render.</param>
        /// <param name="config">Typography settings controlling font, size, spacing, and animation.</param>
        void RenderText(string text, TypographyConfig config);

        /// <summary>
        /// Applies a new typography configuration to already-rendered text without
        /// replacing the content. Used for live condition switching.
        /// </summary>
        /// <param name="config">The new typography settings to apply.</param>
        void ApplyConfig(TypographyConfig config);

        /// <summary>
        /// Highlights a specific word by its index in the rendered text.
        /// Used by word-by-word and guided-reading animation modes.
        /// </summary>
        /// <param name="wordIndex">Zero-based index of the word to highlight.</param>
        void HighlightWord(int wordIndex);

        /// <summary>
        /// Clears any active highlighting without changing the displayed text.
        /// </summary>
        void ClearHighlight();

        /// <summary>
        /// Clears all rendered content from this renderer.
        /// </summary>
        void Clear();
    }
}
