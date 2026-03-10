#nullable enable
using System;
using System.Collections.Generic;

namespace AdapTypeXR.Core.Models
{
    /// <summary>
    /// Represents a single reading study session.
    /// A session encapsulates one participant, one or more passages,
    /// and the sequence of typography conditions applied.
    /// </summary>
    public sealed class ReadingSession
    {
        /// <summary>Globally unique session identifier (GUID).</summary>
        public string SessionId { get; }

        /// <summary>Anonymised participant identifier.</summary>
        public string ParticipantId { get; }

        /// <summary>UTC time when the session was started.</summary>
        public DateTime StartedAt { get; }

        /// <summary>UTC time when the session ended. Null if still in progress.</summary>
        public DateTime? EndedAt { get; private set; }

        /// <summary>Neurodivergent profile of the participant.</summary>
        public NeurodivergentProfile Profile { get; }

        /// <summary>Ordered list of condition IDs applied in this session.</summary>
        public IReadOnlyList<string> ConditionOrder { get; }

        /// <summary>Interpupillary distance in mm, from Varjo calibration.</summary>
        public float InterPupillaryDistanceMm { get; }

        /// <summary>Whether the participant consented to gaze data recording.</summary>
        public bool GazeRecordingConsented { get; }

        /// <summary>Whether the participant consented to physiological data recording.</summary>
        public bool PhysiologicalDataConsented { get; }

        /// <summary>Researcher notes recorded during or after the session.</summary>
        public string ResearcherNotes { get; set; } = string.Empty;

        /// <summary>Application version used for this session.</summary>
        public string AppVersion { get; }

        public ReadingSession(
            string participantId,
            NeurodivergentProfile profile,
            IReadOnlyList<string> conditionOrder,
            float interPupillaryDistanceMm,
            bool gazeRecordingConsented,
            bool physiologicalDataConsented,
            string appVersion)
        {
            SessionId = Guid.NewGuid().ToString();
            ParticipantId = participantId;
            StartedAt = DateTime.UtcNow;
            Profile = profile;
            ConditionOrder = conditionOrder;
            InterPupillaryDistanceMm = interPupillaryDistanceMm;
            GazeRecordingConsented = gazeRecordingConsented;
            PhysiologicalDataConsented = physiologicalDataConsented;
            AppVersion = appVersion;
        }

        /// <summary>
        /// Marks the session as ended. Idempotent — subsequent calls are no-ops.
        /// </summary>
        public void Complete()
        {
            EndedAt ??= DateTime.UtcNow;
        }

        /// <summary>Duration of the session. Returns null if still in progress.</summary>
        public TimeSpan? Duration => EndedAt.HasValue ? EndedAt.Value - StartedAt : null;
    }

    /// <summary>
    /// The neurodivergent profile category of a study participant.
    /// Multiple categories may apply; this enum captures primary profile.
    /// </summary>
    public enum NeurodivergentProfile
    {
        Neurotypical,
        Dyslexia,
        ADHD,
        DyslexiaAndADHD,
        AutismSpectrum,
        Other,
        NotDisclosed
    }
}
