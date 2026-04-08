namespace StravaAPILibary.Models.Uploads
{
    /// <summary>
    /// Represents the status and details of an upload in Strava.
    /// </summary>
    public class Upload
    {
        /// <summary>
        /// The unique identifier of the upload.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The unique identifier of the upload in string format.
        /// </summary>
        public string IdStr { get; set; } = string.Empty;

        /// <summary>
        /// The external identifier of the upload (e.g., original file name or external system reference).
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// The error associated with this upload, if any.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// The current status of the upload.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// The identifier of the activity this upload resulted in.
        /// </summary>
        public long? ActivityId { get; set; }
    }
}
