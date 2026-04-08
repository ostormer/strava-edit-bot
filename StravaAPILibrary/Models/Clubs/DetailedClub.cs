using StravaAPILibary.Models.Enums;

namespace StravaAPILibary.Models.Clubs
{
    /// <summary>
    /// Represents detailed information about a club.
    /// </summary>
    public class DetailedClub
    {
        /// <summary>
        /// The club's unique identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Resource state, indicates level of detail. Possible values: 1 -> "meta", 2 -> "summary", 3 -> "detail".
        /// </summary>
        public int ResourceState { get; set; }

        /// <summary>
        /// The club's name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL to a 60x60 pixel profile picture.
        /// </summary>
        public string ProfileMedium { get; set; } = string.Empty;

        /// <summary>
        /// URL to a ~1185x580 pixel cover photo.
        /// </summary>
        public string CoverPhoto { get; set; } = string.Empty;

        /// <summary>
        /// URL to a ~360x176 pixel cover photo.
        /// </summary>
        public string CoverPhotoSmall { get; set; } = string.Empty;

        /// <summary>
        /// Deprecated. Prefer to use <see cref="ActivityTypes"/>. May take values: cycling, running, triathlon, other.
        /// </summary>
        public string? SportType { get; set; }

        /// <summary>
        /// The activity types that count for a club. This takes precedence over <see cref="SportType"/>.
        /// </summary>
        public List<ActivityType> ActivityTypes { get; set; } = new();

        /// <summary>
        /// The club's city.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// The club's state or geographical region.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// The club's country.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Whether the club is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// The club's member count.
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Whether the club is featured or not.
        /// </summary>
        public bool Featured { get; set; }

        /// <summary>
        /// Whether the club is verified or not.
        /// </summary>
        public bool Verified { get; set; }

        /// <summary>
        /// The club's vanity URL.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// The membership status of the logged-in athlete. May take values: "member", "pending".
        /// </summary>
        public string? Membership { get; set; }

        /// <summary>
        /// Whether the currently logged-in athlete is an administrator of this club.
        /// </summary>
        public bool Admin { get; set; }

        /// <summary>
        /// Whether the currently logged-in athlete is the owner of this club.
        /// </summary>
        public bool Owner { get; set; }

        /// <summary>
        /// The number of athletes in the club that the logged-in athlete follows.
        /// </summary>
        public int FollowingCount { get; set; }
    }
}
