namespace StravaAPILibary.Models.Segments
{
    /// <summary>
    /// Represents the personal record (PR) effort for a segment.
    /// </summary>
    public class SummaryPRSegmentEffort
    {
        /// <summary>
        /// The unique identifier of the activity related to the PR effort.
        /// </summary>
        public long PrActivityId { get; set; }

        /// <summary>
        /// The elapsed time of the PR effort in seconds.
        /// </summary>
        public int PrElapsedTime { get; set; }

        /// <summary>
        /// The date and time when the PR effort was started.
        /// </summary>
        public DateTime PrDate { get; set; }

        /// <summary>
        /// The number of efforts by the authenticated athlete on this segment.
        /// </summary>
        public int EffortCount { get; set; }
    }
}
