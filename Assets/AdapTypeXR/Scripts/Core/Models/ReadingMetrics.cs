using System;

namespace AdapTypeXR.Core.Models
{
    /// <summary>
    /// Aggregated reading behaviour metrics computed for a single passage
    /// under a specific typography condition.
    /// These are the primary dependent variables for the study.
    /// </summary>
    public sealed class ReadingMetrics
    {
        // ── Identity ───────────────────────────────────────────────────────

        /// <summary>ID of the session this metrics record belongs to.</summary>
        public string SessionId { get; }

        /// <summary>ID of the passage read.</summary>
        public string PassageId { get; }

        /// <summary>ID of the typography condition applied.</summary>
        public string ConditionId { get; }

        /// <summary>UTC time the participant began reading this passage.</summary>
        public DateTime ReadingStartedAt { get; }

        /// <summary>UTC time the participant finished reading this passage.</summary>
        public DateTime ReadingEndedAt { get; }

        // ── Eye Movement Metrics ───────────────────────────────────────────

        /// <summary>Total number of fixations recorded during this passage.</summary>
        public int FixationCount { get; init; }

        /// <summary>Mean fixation duration in milliseconds.</summary>
        public float MeanFixationDurationMs { get; init; }

        /// <summary>Number of backward (regressive) saccades.</summary>
        public int RegressionCount { get; init; }

        /// <summary>
        /// Regression rate: regressions / total saccades.
        /// Higher values indicate more reading difficulty.
        /// </summary>
        public float RegressionRate { get; init; }

        /// <summary>Mean progressive saccade amplitude in degrees of visual angle.</summary>
        public float MeanSaccadeAmplitudeDeg { get; init; }

        // ── Pupillometry ───────────────────────────────────────────────────

        /// <summary>Mean pupil diameter in mm across the passage (cognitive load proxy).</summary>
        public float MeanPupilDiameterMm { get; init; }

        /// <summary>Peak pupil diameter in mm (maximal cognitive load moment).</summary>
        public float PeakPupilDiameterMm { get; init; }

        /// <summary>Number of detected blink events.</summary>
        public int BlinkCount { get; init; }

        /// <summary>Blink rate in blinks per minute.</summary>
        public float BlinkRatePerMinute { get; init; }

        // ── Reading Speed ──────────────────────────────────────────────────

        /// <summary>Gross reading speed in words per minute (ignoring regressions).</summary>
        public float GrossReadingSpeedWpm { get; init; }

        /// <summary>
        /// Net reading speed in words per minute, adjusted by comprehension score.
        /// Formula: GrossWpm × (ComprehensionScore / MaxComprehensionScore)
        /// </summary>
        public float NetReadingSpeedWpm { get; init; }

        // ── Comprehension ──────────────────────────────────────────────────

        /// <summary>Raw comprehension score from post-passage questions.</summary>
        public float ComprehensionScore { get; init; }

        /// <summary>Maximum possible comprehension score for this passage.</summary>
        public float MaxComprehensionScore { get; init; }

        /// <summary>Comprehension percentage [0, 1].</summary>
        public float ComprehensionRate => MaxComprehensionScore > 0
            ? ComprehensionScore / MaxComprehensionScore
            : 0f;

        // ── Passage Metadata ───────────────────────────────────────────────

        /// <summary>Word count of the passage.</summary>
        public int PassageWordCount { get; init; }

        /// <summary>Flesch-Kincaid grade level of the passage.</summary>
        public float FleschKincaidGradeLevel { get; init; }

        public ReadingMetrics(
            string sessionId,
            string passageId,
            string conditionId,
            DateTime readingStartedAt,
            DateTime readingEndedAt)
        {
            SessionId = sessionId;
            PassageId = passageId;
            ConditionId = conditionId;
            ReadingStartedAt = readingStartedAt;
            ReadingEndedAt = readingEndedAt;
        }

        /// <summary>Total time spent reading this passage.</summary>
        public TimeSpan ReadingDuration => ReadingEndedAt - ReadingStartedAt;
    }
}
