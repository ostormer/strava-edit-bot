namespace StravaAPILibary.Models.Clubs
{
    /// <summary>
    /// Represents an athlete within a club.
    /// </summary>
    public class ClubAthlete
    {
        /// <summary>
        /// Resource state, indicates level of detail.
        /// 1 -> meta, 2 -> summary, 3 -> detail.
        /// </summary>
        public int ResourceState { get; set; }

        /// <summary>
        /// The athlete's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's last initial.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The athlete's member status.
        /// </summary>
        public string Member { get; set; } = string.Empty;

        /// <summary>
        /// Whether the athlete is a club admin.
        /// </summary>
        public bool Admin { get; set; }

        /// <summary>
        /// Whether the athlete is the club owner.
        /// </summary>
        public bool Owner { get; set; }
    }
}
