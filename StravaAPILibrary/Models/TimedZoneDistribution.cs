using System.Collections.Generic;

namespace StravaAPILibary.Models
{
    /// <summary>
    /// Represents a collection of exclusive ranges (zones) and the time spent in each.
    /// </summary>
    public class TimedZoneDistribution
    {
        /// <summary>
        /// The list of timed zone ranges.
        /// </summary>
        public List<TimedZoneRange> Zones { get; set; } = new();
    }
}
