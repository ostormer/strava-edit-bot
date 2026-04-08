using System.Text.Json.Serialization;

namespace StravaAPILibary.Models
{
    /// <summary>
    /// A roll-up of metrics pertaining to a set of activities.
    /// Values are in seconds and meters.
    /// </summary>
    public class ActivityTotal
    {
        /// <summary>
        /// The number of activities considered in this total.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }

        /// <summary>
        /// The total distance covered by the considered activities (in meters).
        /// </summary>
        [JsonPropertyName("distance")]
        public float Distance { get; set; }

        /// <summary>
        /// The total moving time of the considered activities (in seconds).
        /// </summary>
        [JsonPropertyName("moving_time")]
        public int MovingTime { get; set; }

        /// <summary>
        /// The total elapsed time of the considered activities (in seconds).
        /// </summary>
        [JsonPropertyName("elapsed_time")]
        public int ElapsedTime { get; set; }

        /// <summary>
        /// The total elevation gain of the considered activities (in meters).
        /// </summary>
        [JsonPropertyName("elevation_gain")]
        public float ElevationGain { get; set; }

        /// <summary>
        /// The total number of achievements in the considered activities.
        /// </summary>
        [JsonPropertyName("achievement_count")]
        public int AchievementCount { get; set; }
    }
}
