namespace StravaAPILibary.Models.Photos
{
    /// <summary>
    /// Represents a summary of photos related to an activity.
    /// </summary>
    public class PhotosSummary
    {
        /// <summary>
        /// The number of photos.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The primary photo associated with this activity.
        /// </summary>
        public PhotosSummaryPrimary? Primary { get; set; }
    }
}
