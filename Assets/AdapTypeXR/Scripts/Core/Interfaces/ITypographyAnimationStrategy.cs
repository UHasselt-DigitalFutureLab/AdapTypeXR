using System;
using AdapTypeXR.Core.Models;

namespace AdapTypeXR.Core.Interfaces
{
    /// <summary>
    /// Defines a pluggable text animation strategy.
    /// Follows the Strategy pattern — new animation modes can be added
    /// by implementing this interface without modifying TypographyAnimator.
    /// </summary>
    public interface ITypographyAnimationStrategy
    {
        /// <summary>Human-readable name of this animation mode.</summary>
        string ModeName { get; }

        /// <summary>
        /// Raised when the animation advances to a new word.
        /// Carries the zero-based word index.
        /// </summary>
        event Action<int> WordAdvanced;

        /// <summary>Raised when the animation completes the full text.</summary>
        event Action AnimationCompleted;

        /// <summary>Whether the animation is currently running.</summary>
        bool IsRunning { get; }

        /// <summary>
        /// Initialises the strategy with the text content and configuration
        /// it will animate. Must be called before Start().
        /// </summary>
        /// <param name="text">The full text to animate.</param>
        /// <param name="config">Typography config including WPM and animation parameters.</param>
        void Initialise(string text, TypographyConfig config);

        /// <summary>Starts or resumes the animation.</summary>
        void Start();

        /// <summary>Pauses the animation at the current position.</summary>
        void Pause();

        /// <summary>Stops and resets the animation to the beginning.</summary>
        void Reset();
    }
}
