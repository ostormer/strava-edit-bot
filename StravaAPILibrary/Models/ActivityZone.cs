using System.Collections.Generic;

namespace StravaAPILibary.Models
{
    /// <summary>
    /// Represents an activity zone, such as heart rate or power, including its score, distribution, and related data.
    /// </summary>
    public class ActivityZone
    {
        /// <summary>
        /// The score achieved in this zone.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// The time distribution buckets for this zone.
        /// Each bucket represents the time spent in a specific range.
        /// </summary>
        public List<TimedZoneDistribution> DistributionBuckets { get; set; } = new();

        /// <summary>
        /// The type of the activity zone. 
        /// Valid values: "heartrate" or "power".
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the zone data is sensor-based.
        /// </summary>
        public bool SensorBased { get; set; }

        /// <summary>
        /// The number of points in this zone.
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Indicates if custom zones were used.
        /// </summary>
        public bool CustomZones { get; set; }

        /// <summary>
        /// The maximum value recorded in this zone.
        /// </summary>
        public int Max { get; set; }
    }
}
