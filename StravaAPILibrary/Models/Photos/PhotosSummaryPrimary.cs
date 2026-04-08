namespace StravaAPILibary.Models.Photos
{
    /// <summary>
    /// Represents the primary photo associated with an activity.
    /// </summary>
    public class PhotosSummaryPrimary
    {
        /// <summary>
        /// The ID of the primary photo.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The source of the primary photo.
        /// </summary>
        public int Source { get; set; }

        /// <summary>
        /// The unique identifier of the primary photo.
        /// </summary>
        public string UniqueId { get; set; } = string.Empty;

        /// <summary>
        /// The URLs associated with the primary photo.
        /// </summary>
        public string Urls { get; set; } = string.Empty;
    }
}
