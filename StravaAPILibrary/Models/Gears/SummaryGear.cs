namespace StravaAPILibary.Models.Gears
{
    /// <summary>
    /// Represents a summary of gear associated with an athlete or activity.
    /// </summary>
    public class SummaryGear
    {
        /// <summary>
        /// The gear's unique identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Resource state, indicates level of detail. Possible values: 
        /// 2 = "summary", 3 = "detail".
        /// </summary>
        public int ResourceState { get; set; }

        /// <summary>
        /// Indicates whether this gear is the athlete's primary/default gear.
        /// </summary>
        public bool Primary { get; set; }

        /// <summary>
        /// The gear's display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The distance logged with this gear, in meters.
        /// </summary>
        public float Distance { get; set; }
    }
}
