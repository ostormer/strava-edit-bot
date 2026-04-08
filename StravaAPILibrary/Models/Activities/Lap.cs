using StravaAPILibary.Models.Athletes;
using System;

namespace StravaAPILibary.Models.Activities
{
    /// <summary>
    /// Represents a single lap within an activity.
    /// </summary>
    public class Lap
    {
        /// <summary>
        /// The unique identifier of this lap.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The activity associated with this lap.
        /// </summary>
        public MetaActivity Activity { get; set; } = new MetaActivity();

        /// <summary>
        /// The athlete associated with this lap.
        /// </summary>
        public MetaAthlete Athlete { get; set; } = new MetaAthlete();

        /// <summary>
        /// The lap's average cadence.
        /// </summary>
        public float AverageCadence { get; set; }

        /// <summary>
        /// The lap's average speed.
        /// </summary>
        public float AverageSpeed { get; set; }

        /// <summary>
        /// The lap's distance, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The lap's elapsed time, in seconds.
        /// </summary>
        public int ElapsedTime { get; set; }

        /// <summary>
        /// The start index of this lap in the activity's stream.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// The end index of this lap in the activity's stream.
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// The index of this lap in the activity it belongs to.
        /// </summary>
        public int LapIndex { get; set; }

        /// <summary>
        /// The maximum speed during this lap, in meters per second.
        /// </summary>
        public float MaxSpeed { get; set; }

        /// <summary>
        /// The lap's moving time, in seconds.
        /// </summary>
        public int MovingTime { get; set; }

        /// <summary>
        /// The name of the lap.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's pace zone during this lap.
        /// </summary>
        public int PaceZone { get; set; }

        /// <summary>
        /// The split number for this lap.
        /// </summary>
        public int Split { get; set; }

        /// <summary>
        /// The time at which the lap was started.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The time at which the lap was started in the local timezone.
        /// </summary>
        public DateTime StartDateLocal { get; set; }

        /// <summary>
        /// The elevation gain of this lap, in meters.
        /// </summary>
        public float TotalElevationGain { get; set; }
    }
}
