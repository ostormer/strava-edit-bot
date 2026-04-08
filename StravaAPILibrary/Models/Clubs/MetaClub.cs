namespace StravaAPILibary.Models.Clubs
{
    /// <summary>
    /// Represents a meta-level summary of a club in Strava.
    /// </summary>
    public class MetaClub
    {
        /// <summary>
        /// The club's unique identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Resource state, indicates level of detail.
        /// Possible values:
        /// 1 -> "meta",
        /// 2 -> "summary",
        /// 3 -> "detail".
        /// </summary>
        public int ResourceState { get; set; }

        /// <summary>
        /// The club's name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
