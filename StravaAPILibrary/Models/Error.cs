namespace StravaAPILibary.Models
{
    /// <summary>
    /// Represents an error returned from the Strava API.
    /// </summary>
    public class Error
    {
        /// <summary>
        /// The code associated with this error.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// The specific field or aspect of the resource associated with this error.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// The type of resource associated with this error.
        /// </summary>
        public string Resource { get; set; } = string.Empty;
    }
}
