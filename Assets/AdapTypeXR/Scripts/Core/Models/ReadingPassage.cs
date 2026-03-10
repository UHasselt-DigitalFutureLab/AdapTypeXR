#nullable enable
using System.Collections.Generic;

namespace AdapTypeXR.Core.Models
{
    /// <summary>
    /// A text passage used in the reading study.
    /// Passages are domain-neutral, pre-validated for reading difficulty,
    /// and used across multiple typography conditions.
    /// </summary>
    public sealed class ReadingPassage
    {
        /// <summary>Unique identifier for this passage.</summary>
        public string PassageId { get; }

        /// <summary>Short title displayed in researcher UI (not shown to participants).</summary>
        public string Title { get; }

        /// <summary>The full text of the passage.</summary>
        public string FullText { get; }

        /// <summary>Pages of text, pre-split for the book presenter.</summary>
        public IReadOnlyList<string> Pages { get; }

        /// <summary>Word count of the passage.</summary>
        public int WordCount { get; }

        /// <summary>Flesch-Kincaid grade level (pre-computed).</summary>
        public float FleschKincaidGradeLevel { get; }

        /// <summary>Comprehension questions associated with this passage.</summary>
        public IReadOnlyList<ComprehensionQuestion> Questions { get; }

        /// <summary>Estimated reading time in seconds at 200 WPM (average adult).</summary>
        public float EstimatedReadingTimeSeconds => (WordCount / 200f) * 60f;

        public ReadingPassage(
            string passageId,
            string title,
            string fullText,
            IReadOnlyList<string> pages,
            int wordCount,
            float fleschKincaidGradeLevel,
            IReadOnlyList<ComprehensionQuestion> questions)
        {
            PassageId = passageId;
            Title = title;
            FullText = fullText;
            Pages = pages;
            WordCount = wordCount;
            FleschKincaidGradeLevel = fleschKincaidGradeLevel;
            Questions = questions;
        }
    }

    /// <summary>
    /// A comprehension question associated with a reading passage.
    /// </summary>
    public sealed class ComprehensionQuestion
    {
        public string QuestionId { get; }
        public string QuestionText { get; }
        public QuestionType Type { get; }
        public IReadOnlyList<string> ExpectedKeywords { get; }
        public float MaxScore { get; }

        public ComprehensionQuestion(
            string questionId,
            string questionText,
            QuestionType type,
            IReadOnlyList<string> expectedKeywords,
            float maxScore)
        {
            QuestionId = questionId;
            QuestionText = questionText;
            Type = type;
            ExpectedKeywords = expectedKeywords;
            MaxScore = maxScore;
        }
    }

    /// <summary>Classification of comprehension question types.</summary>
    public enum QuestionType
    {
        /// <summary>Open-ended recall: "Tell me what you remember."</summary>
        FreeRecall,

        /// <summary>Specific factual recall: who, what, where, when.</summary>
        CuedRecall,

        /// <summary>Requires integration across multiple sentences.</summary>
        Inference
    }
}
