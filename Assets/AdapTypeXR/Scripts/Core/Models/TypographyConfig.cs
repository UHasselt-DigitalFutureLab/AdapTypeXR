using UnityEngine;

namespace AdapTypeXR.Core.Models
{
    /// <summary>
    /// Defines all typographic and animation settings for a reading condition.
    /// This is the central configuration object passed between the typography
    /// system, the text renderer, and the data collection layer.
    /// </summary>
    [System.Serializable]
    public sealed class TypographyConfig
    {
        // ── Font & Metrics ─────────────────────────────────────────────────

        /// <summary>Unique identifier for this condition (e.g. "C1-Arial-Static").</summary>
        public string ConditionId;

        /// <summary>Human-readable label shown in researcher UI.</summary>
        public string DisplayName;

        /// <summary>Path to the TextMeshPro font asset (relative to Resources/).</summary>
        public string FontAssetPath;

        /// <summary>Font size in points at the standard reading distance.</summary>
        [Range(12f, 72f)]
        public float FontSize = 28f;

        /// <summary>Line spacing as a multiplier of font size (1.0 = tight, 1.6 = loose).</summary>
        [Range(0.8f, 2.5f)]
        public float LineSpacing = 1.5f;

        /// <summary>Additional letter spacing in em units (0 = default, positive = wider).</summary>
        [Range(-0.1f, 0.3f)]
        public float LetterSpacing = 0.06f;

        /// <summary>Word spacing in em units.</summary>
        [Range(0.1f, 0.6f)]
        public float WordSpacing = 0.30f;

        /// <summary>Paragraph spacing as a multiplier of line height.</summary>
        [Range(0.5f, 3.0f)]
        public float ParagraphSpacing = 1.2f;

        // ── Colours ────────────────────────────────────────────────────────

        /// <summary>Text foreground colour.</summary>
        public Color TextColour = new Color(0.05f, 0.05f, 0.05f, 1f);

        /// <summary>Page/background colour. Off-white (cream) by default to reduce glare.</summary>
        public Color BackgroundColour = new Color(0.98f, 0.96f, 0.90f, 1f);

        /// <summary>Highlight colour used by guided reading animations.</summary>
        public Color HighlightColour = new Color(1.0f, 0.87f, 0.0f, 0.6f);

        // ── Animation ──────────────────────────────────────────────────────

        /// <summary>The animation mode applied to text presentation.</summary>
        public AnimationMode Animation = AnimationMode.None;

        /// <summary>
        /// Reading speed in words per minute.
        /// Used by timed animation modes (RSVP, word-by-word).
        /// </summary>
        [Range(60f, 800f)]
        public float WordsPerMinute = 250f;

        /// <summary>
        /// Whether bionic reading emphasis (bold first half of each word) is applied.
        /// Can be combined with any animation mode.
        /// </summary>
        public bool EnableBionicReading = false;

        /// <summary>
        /// Whether BeeLine Reader colour gradient is applied across lines.
        /// </summary>
        public bool EnableBeeLineGradient = false;

        // ── Research Metadata ──────────────────────────────────────────────

        /// <summary>Free-text research notes associated with this condition.</summary>
        public string ResearchNotes;

        /// <summary>Sprint in which this condition was introduced.</summary>
        public int SprintNumber;
    }

    /// <summary>
    /// Defines how text is animated during a reading session.
    /// Each mode maps to a concrete <see cref="ITypographyAnimationStrategy"/> implementation.
    /// </summary>
    public enum AnimationMode
    {
        /// <summary>All text visible simultaneously; no animation.</summary>
        None,

        /// <summary>
        /// Rapid Serial Visual Presentation — one word at a time at a fixed
        /// screen position. Eliminates saccades; controlled by WordsPerMinute.
        /// </summary>
        RSVP,

        /// <summary>
        /// Full text visible; current word is highlighted progressively.
        /// Preserves context and allows re-reading.
        /// </summary>
        WordByWordHighlight,

        /// <summary>
        /// Text reveals one sentence at a time from left to right.
        /// </summary>
        SentenceReveal,

        /// <summary>
        /// Rhythmic vertical oscillation of the text baseline.
        /// Experimental condition for ADHD reading rhythm entrainment.
        /// </summary>
        BouncingBaseline,

        /// <summary>
        /// Text appears line by line as the participant's gaze approaches the next line.
        /// Gaze-triggered progressive disclosure.
        /// </summary>
        GazeTriggeredReveal
    }
}
