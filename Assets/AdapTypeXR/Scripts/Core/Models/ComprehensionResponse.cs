#nullable enable
using System;

namespace AdapTypeXR.Core.Models
{
    /// <summary>
    /// Records a participant's response to a single comprehension question.
    /// Immutable after construction.
    /// </summary>
    public sealed class ComprehensionResponse
    {
        /// <summary>ID of the session this response belongs to.</summary>
        public string SessionId { get; }

        /// <summary>ID of the passage the question relates to.</summary>
        public string PassageId { get; }

        /// <summary>ID of the comprehension question.</summary>
        public string QuestionId { get; }

        /// <summary>ID of the active typography condition when the question was answered.</summary>
        public string ConditionId { get; }

        /// <summary>The participant's free-text response.</summary>
        public string ResponseText { get; }

        /// <summary>UTC time the response was submitted.</summary>
        public DateTime SubmittedAt { get; }

        /// <summary>Time in seconds the participant spent on this question.</summary>
        public float ResponseTimeSeconds { get; }

        public ComprehensionResponse(
            string sessionId,
            string passageId,
            string questionId,
            string conditionId,
            string responseText,
            DateTime submittedAt,
            float responseTimeSeconds)
        {
            SessionId = sessionId;
            PassageId = passageId;
            QuestionId = questionId;
            ConditionId = conditionId;
            ResponseText = responseText;
            SubmittedAt = submittedAt;
            ResponseTimeSeconds = responseTimeSeconds;
        }
    }
}
