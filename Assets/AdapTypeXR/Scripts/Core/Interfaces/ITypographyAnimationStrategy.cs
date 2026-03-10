using System;
using System.Collections;
using AdapTypeXR.Core.Models;

namespace AdapTypeXR.Core.Interfaces
{
    /// <summary>
    /// Defines a pluggable text animation strategy.
    /// Follows the Strategy pattern — new animation modes can be added
    /// by implementing this interface without modifying TypographyAnimator.
    ///
    /// Strategies are plain C# classes (not MonoBehaviours). The hosting
    /// TypographyAnimator MonoBehaviour runs coroutines on their behalf.
    /// </summary>
    public interface ITypographyAnimationStrategy
    {
        /// <summary>The AnimationMode this strategy handles.</summary>
        AnimationMode AnimationMode { get; }

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
        /// Initialises the strategy with the text and configuration it will animate.
        /// Must be called before <see cref="CreateRoutine"/>.
        /// </summary>
        void Initialise(string text, TypographyConfig config);

        /// <summary>
        /// Returns an IEnumerator that drives the animation.
        /// The hosting MonoBehaviour (TypographyAnimator) calls StartCoroutine on this.
        /// </summary>
        IEnumerator CreateRoutine();

        /// <summary>
        /// Signals the strategy to stop. Called by TypographyAnimator before
        /// StopCoroutine so the strategy can clean up internal state.
        /// </summary>
        void Stop();
    }
}
