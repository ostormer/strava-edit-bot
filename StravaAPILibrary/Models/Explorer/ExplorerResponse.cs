using System.Collections.Generic;

namespace StravaAPILibary.Models.Explorer
{
    /// <summary>
    /// Represents the response from the Explorer API containing a set of segments.
    /// </summary>
    public class ExplorerResponse
    {
        /// <summary>
        /// The set of segments matching an explorer request.
        /// </summary>
        public List<ExplorerSegment> Segments { get; set; } = new List<ExplorerSegment>();
    }
}
