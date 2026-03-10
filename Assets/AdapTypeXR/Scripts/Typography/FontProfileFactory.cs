#nullable enable
using System.Collections.Generic;
using AdapTypeXR.Core.Models;
using UnityEngine;

namespace AdapTypeXR.Typography
{
    /// <summary>
    /// Creates and catalogues all <see cref="TypographyConfig"/> instances
    /// used as study conditions.
    ///
    /// Design pattern: Factory — centralises condition creation so the rest
    /// of the system never constructs TypographyConfig ad hoc.
    ///
    /// Adding a new condition requires only a new method here and a line
    /// in <see cref="BuildDefaultCatalogue"/>. No other class changes.
    /// (Open/Closed Principle)
    /// </summary>
    public static class FontProfileFactory
    {
        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the full catalogue of research conditions for Sprint 0.
        /// Conditions are ordered from most neutral (baseline) to most experimental.
        /// </summary>
        public static IReadOnlyList<TypographyConfig> BuildDefaultCatalogue() =>
            new List<TypographyConfig>
            {
                CreateArialBaseline(),
                CreateOpenDyslexicStatic(),
                CreateAtkinsonHyperlegibleStatic(),
                CreateAtkinsonWordByWord(),
                CreateArialRsvp(),
                CreateAtkinsonBionicReading(),
            };

        /// <summary>Returns a single condition by its ID, or null if not found.</summary>
        public static TypographyConfig? GetById(string conditionId)
        {
            foreach (var config in BuildDefaultCatalogue())
                if (config.ConditionId == conditionId) return config;
            return null;
        }

        // ── Condition Definitions ──────────────────────────────────────────

        /// <summary>
        /// C1 — Arial at standard settings. Neutral baseline for all comparisons.
        /// </summary>
        public static TypographyConfig CreateArialBaseline() => new()
        {
            ConditionId = "C1-Arial-Static",
            DisplayName = "Arial — Static (Baseline)",
            FontAssetPath = "Fonts/Arial SDF",
            FontSize = 28f,
            LineSpacing = 1.5f,
            LetterSpacing = 0.0f,
            WordSpacing = 0.25f,
            ParagraphSpacing = 1.2f,
            TextColour = new Color(0.05f, 0.05f, 0.05f),
            BackgroundColour = new Color(0.98f, 0.96f, 0.90f),
            Animation = AnimationMode.None,
            ResearchNotes = "Neutral baseline. Widely tested in digital reading research. No accessibility modifications.",
            SprintNumber = 0
        };

        /// <summary>
        /// C2 — OpenDyslexic: weighted bottoms, unique letterforms to reduce mirroring.
        /// </summary>
        public static TypographyConfig CreateOpenDyslexicStatic() => new()
        {
            ConditionId = "C2-OpenDyslexic-Static",
            DisplayName = "OpenDyslexic — Static",
            FontAssetPath = "Fonts/OpenDyslexic-Regular SDF",
            FontSize = 28f,
            LineSpacing = 1.6f,
            LetterSpacing = 0.08f,
            WordSpacing = 0.35f,
            ParagraphSpacing = 1.3f,
            TextColour = new Color(0.05f, 0.05f, 0.05f),
            BackgroundColour = new Color(0.98f, 0.96f, 0.90f),
            Animation = AnimationMode.None,
            ResearchNotes = "Dyslexia-targeted font. Weighted bottoms reduce letter reversal. " +
                "Evidence: Rello & Baeza-Yates 2013. Wider spacing applied per crowding research.",
            SprintNumber = 0
        };

        /// <summary>
        /// C3 — Atkinson Hyperlegible: maximally disambiguated letterforms.
        /// </summary>
        public static TypographyConfig CreateAtkinsonHyperlegibleStatic() => new()
        {
            ConditionId = "C3-Atkinson-Static",
            DisplayName = "Atkinson Hyperlegible — Static",
            FontAssetPath = "Fonts/AtkinsonHyperlegible-Regular SDF",
            FontSize = 28f,
            LineSpacing = 1.5f,
            LetterSpacing = 0.06f,
            WordSpacing = 0.30f,
            ParagraphSpacing = 1.2f,
            TextColour = new Color(0.05f, 0.05f, 0.05f),
            BackgroundColour = new Color(0.98f, 0.96f, 0.90f),
            Animation = AnimationMode.None,
            ResearchNotes = "High disambiguation: 1/l/I, 0/O/o distinctions. " +
                "Designed by Braille Institute for low-vision readers. Strong evidence base.",
            SprintNumber = 0
        };

        /// <summary>
        /// C4 — Atkinson Hyperlegible with word-by-word highlight.
        /// Guides attention without removing context or re-reading ability.
        /// </summary>
        public static TypographyConfig CreateAtkinsonWordByWord() => new()
        {
            ConditionId = "C4-Atkinson-WordByWord",
            DisplayName = "Atkinson Hyperlegible — Word-by-Word Highlight",
            FontAssetPath = "Fonts/AtkinsonHyperlegible-Regular SDF",
            FontSize = 28f,
            LineSpacing = 1.5f,
            LetterSpacing = 0.06f,
            WordSpacing = 0.30f,
            ParagraphSpacing = 1.2f,
            TextColour = new Color(0.05f, 0.05f, 0.05f),
            BackgroundColour = new Color(0.98f, 0.96f, 0.90f),
            HighlightColour = new Color(1.0f, 0.87f, 0.0f, 0.6f),
            Animation = AnimationMode.WordByWordHighlight,
            WordsPerMinute = 250f,
            ResearchNotes = "Karaoke-style guided reading. Positive evidence for ADHD " +
                "(reduces lost-place errors). Full text visible — allows regression.",
            SprintNumber = 0
        };

        /// <summary>
        /// C5 — Arial with RSVP at 250 WPM.
        /// Eliminates saccade requirement; tests comprehension under zero oculomotor load.
        /// </summary>
        public static TypographyConfig CreateArialRsvp() => new()
        {
            ConditionId = "C5-Arial-RSVP-250",
            DisplayName = "Arial — RSVP 250 WPM",
            FontAssetPath = "Fonts/Arial SDF",
            FontSize = 36f,
            LineSpacing = 1.0f,
            LetterSpacing = 0.0f,
            WordSpacing = 0.25f,
            ParagraphSpacing = 1.0f,
            TextColour = new Color(0.05f, 0.05f, 0.05f),
            BackgroundColour = new Color(0.98f, 0.96f, 0.90f),
            Animation = AnimationMode.RSVP,
            WordsPerMinute = 250f,
            ResearchNotes = "Rapid Serial Visual Presentation. Eliminates saccades. " +
                "Risk: no re-reading possible. Compare to C1 for oculomotor load hypothesis.",
            SprintNumber = 0
        };

        /// <summary>
        /// C6 — Atkinson Hyperlegible with bionic reading emphasis.
        /// Tests saccade anchoring via bold first-half word emphasis.
        /// </summary>
        public static TypographyConfig CreateAtkinsonBionicReading() => new()
        {
            ConditionId = "C6-Atkinson-Bionic",
            DisplayName = "Atkinson Hyperlegible — Bionic Reading",
            FontAssetPath = "Fonts/AtkinsonHyperlegible-Regular SDF",
            FontSize = 28f,
            LineSpacing = 1.5f,
            LetterSpacing = 0.06f,
            WordSpacing = 0.30f,
            ParagraphSpacing = 1.2f,
            TextColour = new Color(0.05f, 0.05f, 0.05f),
            BackgroundColour = new Color(0.98f, 0.96f, 0.90f),
            Animation = AnimationMode.None,
            EnableBionicReading = true,
            ResearchNotes = "Bold emphasis on first half of each word. Hypothesis: anchors " +
                "saccade landing site. Limited peer review — primary research opportunity.",
            SprintNumber = 0
        };
    }
}
