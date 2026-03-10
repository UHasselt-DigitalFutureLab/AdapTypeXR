#nullable enable
using System.Collections.Generic;

namespace AdapTypeXR.Core.Models
{
    /// <summary>
    /// Lightweight descriptor identifying a font asset and its intended research purpose.
    /// Used by <see cref="Typography.FontProfileFactory"/> to enumerate available fonts
    /// independently of full <see cref="TypographyConfig"/> conditions.
    /// </summary>
    public sealed class FontProfile
    {
        /// <summary>Unique identifier for this font profile.</summary>
        public string ProfileId { get; }

        /// <summary>Human-readable font family name.</summary>
        public string FamilyName { get; }

        /// <summary>Path to the TMP SDF font asset in Resources/.</summary>
        public string AssetPath { get; }

        /// <summary>Whether this font was designed specifically for dyslexic readers.</summary>
        public bool IsDyslexiaTargeted { get; }

        /// <summary>Primary accessibility feature of this font.</summary>
        public string AccessibilityFeature { get; }

        /// <summary>Key evidence source for including this font.</summary>
        public string EvidenceSource { get; }

        public FontProfile(
            string profileId,
            string familyName,
            string assetPath,
            bool isDyslexiaTargeted,
            string accessibilityFeature,
            string evidenceSource)
        {
            ProfileId = profileId;
            FamilyName = familyName;
            AssetPath = assetPath;
            IsDyslexiaTargeted = isDyslexiaTargeted;
            AccessibilityFeature = accessibilityFeature;
            EvidenceSource = evidenceSource;
        }

        /// <summary>Standard catalogue of fonts included in the study.</summary>
        public static IReadOnlyList<FontProfile> StandardCatalogue() =>
            new[]
            {
                new FontProfile("arial", "Arial",
                    "Fonts/Arial SDF",
                    false,
                    "Widely tested neutral baseline",
                    "Extensive digital readability literature"),

                new FontProfile("open-dyslexic", "OpenDyslexic",
                    "Fonts/OpenDyslexic-Regular SDF",
                    true,
                    "Weighted letterform bottoms reduce reversal errors",
                    "Rello & Baeza-Yates 2013"),

                new FontProfile("atkinson-hyperlegible", "Atkinson Hyperlegible",
                    "Fonts/AtkinsonHyperlegible-Regular SDF",
                    false,
                    "Maximal glyph disambiguation (1/l/I, 0/O)",
                    "Braille Institute 2019; Beier et al. 2021"),

                new FontProfile("lexie-readable", "Lexie Readable",
                    "Fonts/LexieReadable-Regular SDF",
                    true,
                    "Clear letterforms, generous spacing",
                    "Marinus et al. 2016"),
            };
    }
}
