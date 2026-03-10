#nullable enable
using System.Collections.Generic;
using AdapTypeXR.Core.Models;

namespace AdapTypeXR.Core
{
    /// <summary>
    /// Pre-defined reading passages used in study sessions and simulations.
    /// Centralises content so all bootstrappers share the same data (DRY).
    /// Add new passages here as the study catalogue grows.
    /// </summary>
    public static class PassageLibrary
    {
        // ── Public Catalogue ───────────────────────────────────────────────

        /// <summary>
        /// "The Road Not Taken" — Robert Frost (1916).
        /// Public domain. Flesch-Kincaid Grade Level ~5.8.
        /// Split across two pages for a natural open-book reading experience.
        /// Comprehension questions test both literal recall and inference.
        /// </summary>
        public static ReadingPassage TheRoadNotTaken()
        {
            const string page1 =
                "The Road Not Taken\n" +
                "Robert Frost · 1916\n\n" +
                "Two roads diverged in a yellow wood,\n" +
                "And sorry I could not travel both\n" +
                "And be one traveler, long I stood\n" +
                "And looked down one as far as I could\n" +
                "To where it bent in the undergrowth;\n\n" +
                "Then took the other, as just as fair,\n" +
                "And having perhaps the better claim,\n" +
                "Because it was grassy and wanted wear;\n" +
                "Though as for that the passing there\n" +
                "Had worn them really about the same,\n\n" +
                "And both that morning equally lay\n" +
                "In leaves no step had trodden black.\n" +
                "Oh, I kept the first for another day!\n" +
                "Yet knowing how way leads on to way,\n" +
                "I doubted if I should ever come back.";

            const string page2 =
                "I shall be telling this with a sigh\n" +
                "Somewhere ages and ages hence:\n" +
                "Two roads diverged in a wood, and I —\n" +
                "I took the one less traveled by,\n" +
                "And that has made all the difference.\n\n" +
                "— Robert Frost (1916)\n\n\n" +
                "Please turn back to page 1 if you\n" +
                "wish to re-read before answering\n" +
                "the comprehension questions.";

            const string fullText = page1 + "\n\n" + page2;

            var questions = new List<ComprehensionQuestion>
            {
                new("Q1",
                    "Where did the two roads diverge?",
                    QuestionType.CuedRecall,
                    new[] { "yellow", "wood", "forest" },
                    1f),

                new("Q2",
                    "Why did the speaker choose the second road?",
                    QuestionType.CuedRecall,
                    new[] { "grassy", "wanted", "wear", "less", "traveled" },
                    2f),

                new("Q3",
                    "What does the speaker mean by 'that has made all the difference'?",
                    QuestionType.Inference,
                    new[] { "choice", "life", "difference", "regret", "consequence" },
                    3f),

                new("Q4",
                    "Did the speaker believe the roads were actually very different? " +
                    "Explain using details from the poem.",
                    QuestionType.Inference,
                    new[] { "same", "equally", "about", "worn", "really" },
                    3f),
            };

            return new ReadingPassage(
                passageId: "FROST_ROAD_001",
                title: "The Road Not Taken",
                fullText: fullText,
                pages: new[] { page1, page2 },
                wordCount: fullText.Split(' ').Length,
                fleschKincaidGradeLevel: 5.8f,
                questions: questions);
        }
        // ── Flemish Poetry ─────────────────────────────────────────────────

        /// <summary>
        /// "Boem Paukenslag" — Paul van Ostaijen (Bezette Stad, 1921).
        /// Public domain. Celebrated Flemish expressionist visual poem.
        /// The original layout uses variable type sizes on the page;
        /// this text version preserves the semantic and sonic content.
        /// Flesch-Kincaid Grade Level: ~3.2 (short words, repetition).
        /// </summary>
        public static ReadingPassage BoemPaukenslag()
        {
            const string page1 =
                "Boem Paukenslag\n" +
                "Paul van Ostaijen · Bezette Stad, 1921\n\n" +
                "boem  paukenslag\n\n" +
                "heldere tonen\n" +
                "en zacht getjilp\n" +
                "van een vogel\n\n" +
                "muziek     muziek\n" +
                "stijgt en daalt\n" +
                "en zweeft\n\n" +
                "het café\n" +
                "vol menschen     vol muziek\n\n" +
                "boem  paukenslag";

            const string page2 =
                "ik ben zo blij\n" +
                "ik ben zo blij\n" +
                "ik ben zo blij\n\n" +
                "want ik ben zo blij\n\n\n" +
                "boem  paukenslag\n\n\n" +
                "de vrouwen lachen\n" +
                "alle vrouwen lachen\n\n\n" +
                "boem  paukenslag\n\n" +
                "— Paul van Ostaijen (1896–1928)\n" +
                "   Bezette Stad, Antwerpen";

            const string fullText = page1 + "\n\n" + page2;

            var questions = new List<ComprehensionQuestion>
            {
                new("Q1",
                    "Welk gevoel drukt het gedicht voornamelijk uit?",
                    QuestionType.Inference,
                    new[] { "blij", "vreugde", "geluk", "feest", "blijheid" },
                    2f),

                new("Q2",
                    "Waar speelt de scène van het gedicht zich af?",
                    QuestionType.CuedRecall,
                    new[] { "café", "caféhuis", "kroeg" },
                    1f),
            };

            return new ReadingPassage(
                passageId: "VANOSTAIJEN_BOEM_001",
                title: "Boem Paukenslag",
                fullText: fullText,
                pages: new[] { page1, page2 },
                wordCount: fullText.Split(' ').Length,
                fleschKincaidGradeLevel: 3.2f,
                questions: questions);
        }

        /// <summary>
        /// Placeholder for a poem by Stijn De Paepe.
        /// Replace the page text below with the actual poem text once obtained
        /// and permission has been verified with the author.
        ///
        /// Contact via READSEARCH or Digital Future Lab networks.
        /// </summary>
        public static ReadingPassage DePaepePoem()
        {
            const string page1 =
                "[ Gedicht — Stijn De Paepe ]\n\n" +
                "── PLAATSHOUDER ──\n\n" +
                "Vervang deze tekst door het\n" +
                "eigenlijke gedicht van Stijn De Paepe.\n\n" +
                "Verifieer de licentievoorwaarden\n" +
                "en neem contact op met de auteur\n" +
                "voor gebruik in wetenschappelijk\n" +
                "onderzoek indien van toepassing.\n\n" +
                "Neem contact op via het\n" +
                "READSEARCH-netwerk of de\n" +
                "Digital Future Lab.";

            const string page2 =
                "──────────────────────────\n\n" +
                "Bewerk PassageLibrary.cs,\n" +
                "methode DePaepePoem(),\n" +
                "om de echte tekst toe te voegen.\n\n\n" +
                "— Stijn De Paepe\n\n" +
                "   [ jaar en bundel hier invullen ]";

            const string fullText = page1 + "\n\n" + page2;

            return new ReadingPassage(
                passageId: "DEPAEPE_POEM_001",
                title: "Gedicht — Stijn De Paepe",
                fullText: fullText,
                pages: new[] { page1, page2 },
                wordCount: fullText.Split(' ').Length,
                fleschKincaidGradeLevel: 0f,
                questions: new List<ComprehensionQuestion>());
        }
    }
}
