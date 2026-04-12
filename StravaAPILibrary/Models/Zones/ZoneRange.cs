namespace StravaAPILibary.Models.Zones
{
    /// <summary>
    /// Represents a single heart rate or power zone range.
    /// </summary>
    public class ZoneRange
    {
        /// <summary>
        /// The minimum value in the range.
        /// </summary>
        public int Min { get; set; }

        /// <summary>
        /// The maximum value in the range.
        /// </summary>
        public int Max { get; set; }
    }
}
