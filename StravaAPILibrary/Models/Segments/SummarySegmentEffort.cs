namespace StravaAPILibary.Models.Segments
{
    /// <summary>
    /// Represents a summary of an athlete's effort on a segment.
    /// </summary>
    public class SummarySegmentEffort
    {
        /// <summary>
        /// The unique identifier of this effort.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The unique identifier of the activity related to this effort.
        /// </summary>
        public long ActivityId { get; set; }

        /// <summary>
        /// The elapsed time of this effort in seconds.
        /// </summary>
        public int ElapsedTime { get; set; }

        /// <summary>
        /// The date and time when this effort was started.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The local date and time when this effort was started.
        /// </summary>
        public DateTime StartDateLocal { get; set; }

        /// <summary>
        /// The distance covered during this effort, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// Indicates whether this effort is the current best on the leaderboard.
        /// </summary>
        public bool IsKom { get; set; }
    }
}
