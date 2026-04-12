namespace StravaAPILibary.Models.Activities
{
    /// <summary>
    /// Represents a split of an activity, typically used for running activities.
    /// </summary>
    public class Split
    {
        /// <summary>
        /// The average speed of this split, in meters per second.
        /// </summary>
        public float AverageSpeed { get; set; }

        /// <summary>
        /// The distance of this split, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The elapsed time of this split, in seconds.
        /// </summary>
        public int ElapsedTime { get; set; }

        /// <summary>
        /// The elevation difference of this split, in meters.
        /// </summary>
        public float ElevationDifference { get; set; }

        /// <summary>
        /// The pacing zone of this split.
        /// </summary>
        public int PaceZone { get; set; }

        /// <summary>
        /// The moving time of this split, in seconds.
        /// </summary>
        public int MovingTime { get; set; }

        /// <summary>
        /// The index number of this split.
        /// </summary>
        public int SplitIndex { get; set; }
    }
}
