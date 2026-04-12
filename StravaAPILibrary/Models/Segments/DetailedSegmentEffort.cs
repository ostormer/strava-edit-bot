using StravaAPILibary.Models.Athletes;
using StravaAPILibary.Models.Activities;
using System;

namespace StravaAPILibary.Models.Segments
{
    /// <summary>
    /// Represents a detailed segment effort, including performance metrics and related activity/athlete information.
    /// </summary>
    public class DetailedSegmentEffort
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
        /// The elapsed time of the effort, in seconds.
        /// </summary>
        public int ElapsedTime { get; set; }

        /// <summary>
        /// The time at which the effort was started.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The local time at which the effort was started.
        /// </summary>
        public DateTime StartDateLocal { get; set; }

        /// <summary>
        /// The distance of the effort, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// Indicates whether this effort is the current best on the leaderboard (KOM/QOM).
        /// </summary>
        public bool IsKom { get; set; }

        /// <summary>
        /// The name of the segment on which this effort was performed.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The activity associated with this effort.
        /// </summary>
        public MetaActivity? Activity { get; set; }

        /// <summary>
        /// The athlete who performed this effort.
        /// </summary>
        public MetaAthlete? Athlete { get; set; }

        /// <summary>
        /// The moving time of the effort, in seconds.
        /// </summary>
        public int MovingTime { get; set; }

        /// <summary>
        /// The start index of this effort in its activity's stream.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// The end index of this effort in its activity's stream.
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// The average cadence during this effort.
        /// </summary>
        public float AverageCadence { get; set; }

        /// <summary>
        /// The average power output during this effort, in watts.
        /// </summary>
        public float AverageWatts { get; set; }

        /// <summary>
        /// Indicates whether the wattage was measured by a power meter (true) or estimated (false).
        /// </summary>
        public bool DeviceWatts { get; set; }

        /// <summary>
        /// The average heart rate of the athlete during this effort.
        /// </summary>
        public float AverageHeartrate { get; set; }

        /// <summary>
        /// The maximum heart rate of the athlete during this effort.
        /// </summary>
        public float MaxHeartrate { get; set; }

        /// <summary>
        /// The segment associated with this effort.
        /// </summary>
        public SummarySegment? Segment { get; set; }

        /// <summary>
        /// The athlete's rank on the global leaderboard (if in the top 10 at the time of upload).
        /// </summary>
        public int? KomRank { get; set; }

        /// <summary>
        /// The athlete's rank on their personal leaderboard (if in the top 3 at the time of upload).
        /// </summary>
        public int? PrRank { get; set; }

        /// <summary>
        /// Indicates whether this effort should be hidden when viewed within an activity.
        /// </summary>
        public bool Hidden { get; set; }
    }
}
