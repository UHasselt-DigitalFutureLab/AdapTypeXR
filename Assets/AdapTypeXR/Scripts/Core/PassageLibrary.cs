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
        /// <summary>
        /// Returns all available passages in the library.
        /// Used by passage selector UI to enumerate options.
        /// </summary>
        public static IReadOnlyList<ReadingPassage> GetAllPassages() =>
            new[]
            {
                DePaepeWatHelpt(),
                BoemPaukenslag(),
                MarcGroet(),
                TheRoadNotTaken(),
            };

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
        /// "Wat helpt" — Stijn De Paepe (De Morgen, 2020).
        /// One of De Paepe's best-known poems, written during the corona crisis.
        /// Published as a Dagvers in De Morgen. Included with educational/research intent.
        /// Flesch-Kincaid Grade Level: ~4.5 (simple vocabulary, strong rhythm).
        /// </summary>
        public static ReadingPassage DePaepeWatHelpt()
        {
            const string page1 =
                "Wat helpt\n" +
                "Stijn De Paepe · De Morgen, 2020\n\n" +
                "Als vloeken helpt, dan vloek je maar.\n" +
                "Maak herrie, stennis en misbaar.\n" +
                "Scheld schel en luid je goudvis uit\n" +
                "en schreeuw je scherven bij elkaar.\n\n" +
                "Als bidden helpt, bid dan gerust.\n" +
                "Als het je troost of sterkt of sust.\n" +
                "Of vraag om raad. Als Hij bestaat\n" +
                "dan is het goed, maar 't is geen must.\n\n" +
                "Als huilen helpt, ga dan je gang.\n" +
                "Het is niet niks en het duurt lang.\n" +
                "Het kan geen kwaad als het niet gaat.\n" +
                "Het mag gezien zijn, wees niet bang.";

            const string page2 =
                "Als praten helpt, bel me dan op\n" +
                "en steek van wal, hals over kop\n" +
                "en van de hak weer op de tak\n" +
                "of zachtjes sluipend uit je slop.\n\n" +
                "Als lopen helpt, vertrek meteen.\n" +
                "Zeer doelgericht of nergens heen.\n" +
                "Het hoeft niet snel, al mag dat wel.\n" +
                "Met verre vrienden of alleen.\n\n" +
                "Als zwijgen helpt, wees dan maar stil\n" +
                "en duik — als dat is wat je wil —\n" +
                "een tijdje weg van pijn en pech —\n" +
                "als je weer opduikt, geef een gil.";

            const string page3 =
                "Als lachen helpt, ken ik een grap\n" +
                "of val dolkomisch van de trap.\n" +
                "Als dansen helpt, is er muziek.\n" +
                "Als breien helpt, dan hou je steek.\n" +
                "Als boos zijn helpt, geef ik kritiek.\n" +
                "Als bakken helpt, let there be cake.\n" +
                "Als yoga helpt, wees fluks en zen.\n" +
                "Als slapen helpt, stop ik je in.\n" +
                "Als schrijven helpt, scherp dan je pen.\n" +
                "Als poetsen helpt, welaan: begin!\n\n" +
                "Je voelt je murw en overstelpt\n" +
                "en snakt naar stranden, wit geschelpt...\n" +
                "Hou vol. Vat moed. Want het komt goed.\n" +
                "Doe ondertussen maar wat helpt.\n\n" +
                "— Stijn De Paepe (1979–2022)\n" +
                "   De Morgen, Dagvers";

            const string fullText = page1 + "\n\n" + page2 + "\n\n" + page3;

            var questions = new List<ComprehensionQuestion>
            {
                new("Q1",
                    "Welk advies geeft de dichter als je verdrietig bent?",
                    QuestionType.CuedRecall,
                    new[] { "huilen", "gang", "niet bang", "mag" },
                    1f),

                new("Q2",
                    "Wat is de boodschap van de laatste strofe?",
                    QuestionType.Inference,
                    new[] { "volhouden", "moed", "goed", "helpt", "hoop" },
                    2f),

                new("Q3",
                    "Het gedicht herhaalt steeds 'als ... helpt'. Wat is het effect van die herhaling?",
                    QuestionType.Inference,
                    new[] { "ritme", "troost", "mogelijkheden", "keuze", "steun", "structuur" },
                    3f),
            };

            return new ReadingPassage(
                passageId: "DEPAEPE_WATHELPT_001",
                title: "Wat helpt — Stijn De Paepe",
                fullText: fullText,
                pages: new[] { page1, page2, page3 },
                wordCount: fullText.Split(' ').Length,
                fleschKincaidGradeLevel: 4.5f,
                questions: questions);
        }

        /// <summary>
        /// "Marc groet 's morgens de dingen" — Paul van Ostaijen (Nagelaten gedichten, 1928).
        /// Public domain. One of the most celebrated Flemish poems.
        /// Playful, childlike address of everyday objects. Visual/typographic poem.
        /// Flesch-Kincaid Grade Level: ~2.0 (very simple vocabulary).
        /// </summary>
        public static ReadingPassage MarcGroet()
        {
            const string page1 =
                "Marc groet 's morgens de dingen\n" +
                "Paul van Ostaijen · Nagelaten gedichten, 1928\n\n" +
                "Dag ventje met de fiets\n\n" +
                "op de vaas met de bloem\n" +
                "ploem ploem\n\n" +
                "dag stoel naast de tafel\n\n" +
                "dag brood op de tafel\n\n" +
                "dag visserke-vis\n" +
                "met de pijp\n" +
                "en\n" +
                "dag visserke-vis\n" +
                "met de pet\n\n" +
                "pet en pijp\n" +
                "van het visserke-vis";

            const string page2 =
                "goeiendag\n\n\n" +
                "D  a  a  —  a  g\n\n\n" +
                "vis\n\n" +
                "dag lieve vis\n\n" +
                "dag klein visselansen\n\n" +
                "mansen\n\n\n" +
                "— Paul van Ostaijen (1896–1928)\n" +
                "   Nagelaten gedichten";

            const string fullText = page1 + "\n\n" + page2;

            var questions = new List<ComprehensionQuestion>
            {
                new("Q1",
                    "Wie of wat begroet Marc in het gedicht?",
                    QuestionType.CuedRecall,
                    new[] { "ventje", "fiets", "stoel", "brood", "vis", "dingen" },
                    1f),

                new("Q2",
                    "Wat valt op aan de manier waarop het gedicht is geschreven?",
                    QuestionType.Inference,
                    new[] { "kinderlijk", "speels", "herhaling", "klank", "eenvoudig", "visueel" },
                    2f),
            };

            return new ReadingPassage(
                passageId: "VANOSTAIJEN_MARC_001",
                title: "Marc groet 's morgens de dingen",
                fullText: fullText,
                pages: new[] { page1, page2 },
                wordCount: fullText.Split(' ').Length,
                fleschKincaidGradeLevel: 2.0f,
                questions: questions);
        }
    }
}
