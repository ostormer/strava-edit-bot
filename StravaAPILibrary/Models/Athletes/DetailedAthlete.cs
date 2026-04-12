using StravaAPILibary.Models.Clubs;
using StravaAPILibary.Models.Gears;

namespace StravaAPILibary.Models.Athletes
{
    /// <summary>
    /// Represents a detailed athlete profile with extensive personal, equipment, and club data.
    /// </summary>
    public class DetailedAthlete
    {
        /// <summary>
        /// The unique identifier of the athlete.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Resource state, indicates level of detail. Possible values: 1 -> "meta", 2 -> "summary", 3 -> "detail".
        /// </summary>
        public int ResourceState { get; set; }

        /// <summary>
        /// The athlete's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

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
        public string? City { get; set; }

        /// <summary>
        /// The athlete's state or geographical region.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// The athlete's country.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// The athlete's sex. May take one of the following values: "M", "F".
        /// </summary>
        public string? Sex { get; set; }

        /// <summary>
        /// Deprecated. Use <see cref="Summit"/> instead. Whether the athlete has any Summit subscription.
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

        /// <summary>
        /// The athlete's follower count.
        /// </summary>
        public int FollowerCount { get; set; }

        /// <summary>
        /// The athlete's friend count.
        /// </summary>
        public int FriendCount { get; set; }

        /// <summary>
        /// The athlete's preferred unit system. May take one of the following values: "feet", "meters".
        /// </summary>
        public string MeasurementPreference { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's Functional Threshold Power (FTP).
        /// </summary>
        public int? Ftp { get; set; }

        /// <summary>
        /// The athlete's weight.
        /// </summary>
        public float? Weight { get; set; }

        /// <summary>
        /// The clubs the athlete is a member of.
        /// </summary>
        public List<SummaryClub> Clubs { get; set; } = new();

        /// <summary>
        /// The bikes owned by the athlete.
        /// </summary>
        public List<SummaryGear> Bikes { get; set; } = new();

        /// <summary>
        /// The shoes owned by the athlete.
        /// </summary>
        public List<SummaryGear> Shoes { get; set; } = new();
    }
}
