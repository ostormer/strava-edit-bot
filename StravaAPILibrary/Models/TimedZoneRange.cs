namespace StravaAPILibary.Models
{
    /// <summary>
    /// Represents the time spent in a specific zone range.
    /// </summary>
    public class TimedZoneRange
    {
        /// <summary>
        /// The minimum value in the range.
        /// </summary>
        public int Min { get; set; }

        /// <summary>
        /// The maximum value in the range.
        /// </summary>
        public int Max { get; set; }

        /// <summary>
        /// The number of seconds spent in this zone.
        /// </summary>
        public int Time { get; set; }
    }
}
