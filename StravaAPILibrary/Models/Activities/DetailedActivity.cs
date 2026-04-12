using StravaAPILibary.Models.Athletes;
using StravaAPILibary.Models.Enums;
using StravaAPILibary.Models.Common;
using StravaAPILibary.Models.Gears;
using StravaAPILibary.Models.Maps;
using StravaAPILibary.Models.Photos;
using StravaAPILibary.Models.Segments;
using StravaAPILibary.Models.Activities; 


namespace StravaAPILibary.Models.Activities
{
    /// <summary>
    /// Represents a detailed Strava activity with extensive metadata, statistics, and related objects.
    /// </summary>
    public class DetailedActivity
    {
        /// <summary>
        /// The unique identifier of the activity.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The identifier provided at upload time.
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// The identifier of the upload that resulted in this activity.
        /// </summary>
        public long UploadId { get; set; }

        /// <summary>
        /// The athlete associated with this activity.
        /// </summary>
        public MetaAthlete Athlete { get; set; } = new();

        /// <summary>
        /// The name of the activity.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The activity's distance in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The activity's moving time in seconds.
        /// </summary>
        public int MovingTime { get; set; }

        /// <summary>
        /// The activity's elapsed time in seconds.
        /// </summary>
        public int ElapsedTime { get; set; }

        /// <summary>
        /// The activity's total elevation gain in meters.
        /// </summary>
        public float TotalElevationGain { get; set; }

        /// <summary>
        /// The activity's highest elevation in meters.
        /// </summary>
        public float ElevHigh { get; set; }

        /// <summary>
        /// The activity's lowest elevation in meters.
        /// </summary>
        public float ElevLow { get; set; }

        /// <summary>
        /// Deprecated. Prefer to use <see cref="SportType"/>.
        /// </summary>
        public ActivityType Type { get; set; }

        /// <summary>
        /// The sport type of the activity.
        /// </summary>
        public SportType SportType { get; set; }

        /// <summary>
        /// The UTC time when the activity was started.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The local time when the activity was started.
        /// </summary>
        public DateTime StartDateLocal { get; set; }

        /// <summary>
        /// The timezone of the activity.
        /// </summary>
        public string Timezone { get; set; } = string.Empty;

        /// <summary>
        /// The start latitude/longitude of the activity.
        /// </summary>
        public LatLng? StartLatLng { get; set; }

        /// <summary>
        /// The end latitude/longitude of the activity.
        /// </summary>
        public LatLng? EndLatLng { get; set; }

        /// <summary>
        /// The number of achievements gained during this activity.
        /// </summary>
        public int AchievementCount { get; set; }

        /// <summary>
        /// The number of kudos given for this activity.
        /// </summary>
        public int KudosCount { get; set; }

        /// <summary>
        /// The number of comments for this activity.
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// The number of athletes participating in this activity.
        /// </summary>
        public int AthleteCount { get; set; }

        /// <summary>
        /// The number of Instagram photos for this activity.
        /// </summary>
        public int PhotoCount { get; set; }

        /// <summary>
        /// The total number of photos (Instagram + Strava) for this activity.
        /// </summary>
        public int TotalPhotoCount { get; set; }

        /// <summary>
        /// The map associated with this activity.
        /// </summary>
        public PolylineMap Map { get; set; } = new();

        /// <summary>
        /// Whether this activity was recorded on a training machine.
        /// </summary>
        public bool Trainer { get; set; }

        /// <summary>
        /// Whether this activity is a commute.
        /// </summary>
        public bool Commute { get; set; }

        /// <summary>
        /// Whether this activity was created manually.
        /// </summary>
        public bool Manual { get; set; }

        /// <summary>
        /// Whether this activity is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// Whether this activity is flagged.
        /// </summary>
        public bool Flagged { get; set; }

        /// <summary>
        /// The workout type of the activity.
        /// </summary>
        public int? WorkoutType { get; set; }

        /// <summary>
        /// The upload identifier in string format.
        /// </summary>
        public string? UploadIdStr { get; set; }

        /// <summary>
        /// The average speed in meters per second.
        /// </summary>
        public float AverageSpeed { get; set; }

        /// <summary>
        /// The maximum speed in meters per second.
        /// </summary>
        public float MaxSpeed { get; set; }

        /// <summary>
        /// Whether the logged-in athlete has kudoed this activity.
        /// </summary>
        public bool HasKudoed { get; set; }

        /// <summary>
        /// Whether this activity is muted in the home feed.
        /// </summary>
        public bool HideFromHome { get; set; }

        /// <summary>
        /// The gear ID used for this activity.
        /// </summary>
        public string? GearId { get; set; }

        /// <summary>
        /// The total work done in kilojoules during this activity (rides only).
        /// </summary>
        public float? Kilojoules { get; set; }

        /// <summary>
        /// Average power output in watts (rides only).
        /// </summary>
        public float? AverageWatts { get; set; }

        /// <summary>
        /// Whether the watts are from a power meter (false if estimated).
        /// </summary>
        public bool? DeviceWatts { get; set; }

        /// <summary>
        /// Maximum power in watts (rides with power meter only).
        /// </summary>
        public int? MaxWatts { get; set; }

        /// <summary>
        /// Weighted average watts (Normalized Power) for rides with power data.
        /// </summary>
        public int? WeightedAverageWatts { get; set; }

        /// <summary>
        /// Description of the activity.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Photos associated with the activity.
        /// </summary>
        public PhotosSummary? Photos { get; set; }

        /// <summary>
        /// Gear summary used during the activity.
        /// </summary>
        public SummaryGear? Gear { get; set; }

        /// <summary>
        /// Calories burned during the activity.
        /// </summary>
        public float? Calories { get; set; }

        /// <summary>
        /// Segment efforts during this activity.
        /// </summary>
        public List<DetailedSegmentEffort> SegmentEfforts { get; set; } = new();

        /// <summary>
        /// The device used to record this activity.
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// The token used to embed this activity.
        /// </summary>
        public string? EmbedToken { get; set; }

        /// <summary>
        /// Splits of the activity in metric units (for runs).
        /// </summary>
        public List<Split>? SplitsMetric { get; set; }

        /// <summary>
        /// Splits of the activity in imperial units (for runs).
        /// </summary>
        public List<Split>? SplitsStandard { get; set; }

        /// <summary>
        /// Laps of the activity.
        /// </summary>
        public List<Lap>? Laps { get; set; }

        /// <summary>
        /// Best efforts during this activity.
        /// </summary>
        public List<DetailedSegmentEffort>? BestEfforts { get; set; }
    }
}
