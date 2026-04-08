using System.Text.Json.Serialization;

namespace StravaAPILibary.Models
{
    /// <summary>
    /// Represents rolled-up statistics and totals for an athlete,
    /// including recent, year-to-date, and all-time metrics for rides, runs, and swims.
    /// </summary>
    public class ActivityStats
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityStats"/> class
        /// with default values for all activity totals.
        /// </summary>
        public ActivityStats()
        {
            RecentRideTotals = new ActivityTotal();
            RecentRunTotals = new ActivityTotal();
            RecentSwimTotals = new ActivityTotal();
            YtdRideTotals = new ActivityTotal();
            YtdRunTotals = new ActivityTotal();
            YtdSwimTotals = new ActivityTotal();
            AllRideTotals = new ActivityTotal();
            AllRunTotals = new ActivityTotal();
            AllSwimTotals = new ActivityTotal();
        }

        /// <summary>
        /// The longest distance ridden by the athlete, in meters.
        /// </summary>
        [JsonPropertyName("biggest_ride_distance")]
        public double BiggestRideDistance { get; set; }

        /// <summary>
        /// The highest climb elevation gain achieved by the athlete, in meters.
        /// </summary>
        [JsonPropertyName("biggest_climb_elevation_gain")]
        public double BiggestClimbElevationGain { get; set; }

        /// <summary>
        /// The recent (last 4 weeks) ride statistics for the athlete.
        /// </summary>
        [JsonPropertyName("recent_ride_totals")]
        public ActivityTotal RecentRideTotals { get; set; }

        /// <summary>
        /// The recent (last 4 weeks) run statistics for the athlete.
        /// </summary>
        [JsonPropertyName("recent_run_totals")]
        public ActivityTotal RecentRunTotals { get; set; }

        /// <summary>
        /// The recent (last 4 weeks) swim statistics for the athlete.
        /// </summary>
        [JsonPropertyName("recent_swim_totals")]
        public ActivityTotal RecentSwimTotals { get; set; }

        /// <summary>
        /// The year-to-date ride statistics for the athlete.
        /// </summary>
        [JsonPropertyName("ytd_ride_totals")]
        public ActivityTotal YtdRideTotals { get; set; }

        /// <summary>
        /// The year-to-date run statistics for the athlete.
        /// </summary>
        [JsonPropertyName("ytd_run_totals")]
        public ActivityTotal YtdRunTotals { get; set; }

        /// <summary>
        /// The year-to-date swim statistics for the athlete.
        /// </summary>
        [JsonPropertyName("ytd_swim_totals")]
        public ActivityTotal YtdSwimTotals { get; set; }

        /// <summary>
        /// The all-time ride statistics for the athlete.
        /// </summary>
        [JsonPropertyName("all_ride_totals")]
        public ActivityTotal AllRideTotals { get; set; }

        /// <summary>
        /// The all-time run statistics for the athlete.
        /// </summary>
        [JsonPropertyName("all_run_totals")]
        public ActivityTotal AllRunTotals { get; set; }

        /// <summary>
        /// The all-time swim statistics for the athlete.
        /// </summary>
        [JsonPropertyName("all_swim_totals")]
        public ActivityTotal AllSwimTotals { get; set; }
    }
}
