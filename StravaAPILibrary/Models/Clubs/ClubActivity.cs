using StravaAPILibary.Models.Athletes;
using StravaAPILibary.Models.Enums;

namespace StravaAPILibary.Models.Clubs
{
    /// <summary>
    /// Represents an activity within a club.
    /// </summary>
    public class ClubActivity
    {
        /// <summary>
        /// The athlete who performed the activity.
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
        /// The type of the activity. (Deprecated - prefer SportType)
        /// </summary>
        public ActivityType Type { get; set; }

        /// <summary>
        /// The sport type of the activity.
        /// </summary>
        public SportType SportType { get; set; }

        /// <summary>
        /// The activity's workout type.
        /// </summary>
        public int WorkoutType { get; set; }
    }
}
