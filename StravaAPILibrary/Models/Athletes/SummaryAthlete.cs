using System;

namespace StravaAPILibary.Models.Athletes
{
    /// <summary>
    /// Represents a summary of an athlete with profile and location details.
    /// </summary>
    public class SummaryAthlete
    {
        /// <summary>
        /// The unique identifier of the athlete.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Resource state, indicates level of detail.
        /// Possible values: 1 (meta), 2 (summary), 3 (detail).
        /// </summary>
        public int ResourceState { get; set; }

        /// <summary>
        /// The athlete's first name.
        /// </summary>
        public string Firstname { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's last name.
        /// </summary>
        public string Lastname { get; set; } = string.Empty;

        /// <summary>
        /// URL to a 62x62 pixel profile picture.
        /// </summary>
        public string ProfileMedium { get; set; } = string.Empty;

        /// <summary>
        /// URL to a 124x124 pixel profile picture.
        /// </summary>
        public string Profile { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's city.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's state or geographical region.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's country.
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's sex. M = Male, F = Female.
        /// </summary>
        public string Sex { get; set; } = string.Empty;

        /// <summary>
        /// Deprecated. Use Summit field instead. Whether the athlete has any Summit subscription.
        /// </summary>
        public bool Premium { get; set; }

        /// <summary>
        /// Whether the athlete has any Summit subscription.
        /// </summary>
        public bool Summit { get; set; }

        /// <summary>
        /// The time at which the athlete was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The time at which the athlete was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
