using StravaAPILibary.Models.Enums;

namespace StravaAPILibary.Models.Activities
{
    /// <summary>
    /// Represents an updatable activity in Strava.
    /// </summary>
    public class UpdatableActivity
    {
        /// <summary>
        /// Indicates whether this activity is a commute.
        /// </summary>
        public bool Commute { get; set; }

        /// <summary>
        /// Indicates whether this activity was recorded on a training machine.
        /// </summary>
        public bool Trainer { get; set; }

        /// <summary>
        /// Indicates whether this activity is muted (hidden from home feed).
        /// </summary>
        public bool HideFromHome { get; set; }

        /// <summary>
        /// The description of the activity.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The name of the activity.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The deprecated activity type. Prefer to use <see cref="SportType"/>.
        /// </summary>
        public ActivityType? Type { get; set; }

        /// <summary>
        /// The sport type of the activity.
        /// </summary>
        public SportType SportType { get; set; }

        /// <summary>
        /// Identifier for the gear associated with the activity. 
        /// Use 'none' to clear gear from activity.
        /// </summary>
        public string GearId { get; set; } = string.Empty;
    }
}
