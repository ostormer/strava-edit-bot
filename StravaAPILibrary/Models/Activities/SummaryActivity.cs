using StravaAPILibary.Models.Athletes;
using StravaAPILibary.Models.Enums;
using StravaAPILibary.Models.Common;
using StravaAPILibary.Models.Maps;
using System;

namespace StravaAPILibary.Models.Activities
{
    /// <summary>
    /// Represents a summarized Strava activity with key performance and metadata fields.
    /// </summary>
    public class SummaryActivity
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
        public long? UploadId { get; set; }

        /// <summary>
        /// The athlete who performed this activity.
        /// </summary>
        public MetaAthlete? Athlete { get; set; }

        /// <summary>
        /// The name of the activity.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The activity's distance, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The activity's moving time, in seconds.
        /// </summary>
        public int MovingTime { get; set; }

        /// <summary>
        /// The activity's elapsed time, in seconds.
        /// </summary>
        public int ElapsedTime { get; set; }

        /// <summary>
        /// The total elevation gain of the activity, in meters.
        /// </summary>
        public float TotalElevationGain { get; set; }

        /// <summary>
        /// The highest elevation reached during the activity, in meters.
        /// </summary>
        public float ElevHigh { get; set; }

        /// <summary>
        /// The lowest elevation reached during the activity, in meters.
        /// </summary>
        public float ElevLow { get; set; }

        /// <summary>
        /// The deprecated activity type. Prefer <see cref="SportType"/>.
        /// </summary>
        public ActivityType? Type { get; set; }

        /// <summary>
        /// The sport type of the activity.
        /// </summary>
        public SportType SportType { get; set; }

        /// <summary>
        /// The time at which the activity was started (UTC).
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The local time at which the activity was started.
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
        /// The number of comments on this activity.
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// The number of athletes participating in this activity (group activities).
        /// </summary>
        public int AthleteCount { get; set; }

        /// <summary>
        /// The number of Instagram photos for this activity.
        /// </summary>
        public int PhotoCount { get; set; }

        /// <summary>
        /// The total number of Instagram and Strava photos for this activity.
        /// </summary>
        public int TotalPhotoCount { get; set; }

        /// <summary>
        /// The map information associated with this activity.
        /// </summary>
        public PolylineMap? Map { get; set; }

        /// <summary>
        /// Indicates whether this activity was recorded on a training machine.
        /// </summary>
        public bool Trainer { get; set; }

        /// <summary>
        /// Indicates whether this activity is a commute.
        /// </summary>
        public bool Commute { get; set; }

        /// <summary>
        /// Indicates whether this activity was created manually.
        /// </summary>
        public bool Manual { get; set; }

        /// <summary>
        /// Indicates whether this activity is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// Indicates whether this activity was flagged.
        /// </summary>
        public bool Flagged { get; set; }

        /// <summary>
        /// The workout type of this activity.
        /// </summary>
        public int? WorkoutType { get; set; }

        /// <summary>
        /// The unique identifier of the upload in string format.
        /// </summary>
        public string? UploadIdStr { get; set; }

        /// <summary>
        /// The average speed of this activity, in meters per second.
        /// </summary>
        public float AverageSpeed { get; set; }

        /// <summary>
        /// The maximum speed of this activity, in meters per second.
        /// </summary>
        public float MaxSpeed { get; set; }

        /// <summary>
        /// Indicates whether the authenticated athlete has given kudos to this activity.
        /// </summary>
        public bool HasKudoed { get; set; }

        /// <summary>
        /// Indicates whether the activity is muted (hidden from home feed).
        /// </summary>
        public bool HideFromHome { get; set; }

        /// <summary>
        /// The gear ID associated with this activity.
        /// </summary>
        public string? GearId { get; set; }

        /// <summary>
        /// The total work done in kilojoules during this activity (rides only).
        /// </summary>
        public float? Kilojoules { get; set; }

        /// <summary>
        /// The average power output in watts (rides only).
        /// </summary>
        public float? AverageWatts { get; set; }

        /// <summary>
        /// Indicates whether the power data is from a power meter (true) or estimated (false).
        /// </summary>
        public bool? DeviceWatts { get; set; }

        /// <summary>
        /// The maximum power output during this activity (rides with power meter only).
        /// </summary>
        public int? MaxWatts { get; set; }

        /// <summary>
        /// Similar to Normalized Power, rides with power meter data only.
        /// </summary>
        public int? WeightedAverageWatts { get; set; }
    }
}
