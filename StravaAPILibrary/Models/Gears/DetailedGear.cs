namespace StravaAPILibary.Models.Gears
{
    /// <summary>
    /// Represents detailed information about a piece of gear in Strava.
    /// </summary>
    public class DetailedGear
    {
        /// <summary>
        /// The gear's unique identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Resource state, indicates level of detail. Possible values: 2 -> "summary", 3 -> "detail".
        /// </summary>
        public int ResourceState { get; set; }

        /// <summary>
        /// Whether this gear is the owner's default gear.
        /// </summary>
        public bool Primary { get; set; }

        /// <summary>
        /// The gear's name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The distance logged with this gear.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The gear's brand name.
        /// </summary>
        public string BrandName { get; set; } = string.Empty;

        /// <summary>
        /// The gear's model name.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// The gear's frame type (bike only).
        /// </summary>
        public int FrameType { get; set; }

        /// <summary>
        /// The description of the gear.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
