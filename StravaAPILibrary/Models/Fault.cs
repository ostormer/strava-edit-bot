using System.Collections.Generic;

namespace StravaAPILibary.Models
{
    /// <summary>
    /// Represents a fault response from the Strava API, including a message and associated errors.
    /// </summary>
    public class Fault
    {
        /// <summary>
        /// The message of the fault.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The set of specific errors associated with this fault, if any.
        /// </summary>
        public List<Error> Errors { get; set; } = new List<Error>();
    }
}
