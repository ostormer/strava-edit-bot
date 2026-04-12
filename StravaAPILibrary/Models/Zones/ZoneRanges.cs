using System.Collections.Generic;

namespace StravaAPILibary.Models.Zones
{
    /// <summary>
    /// A collection of zone ranges (e.g., heart rate or power zones).
    /// </summary>
    public class ZoneRanges
    {
        /// <summary>
        /// A list of individual zone ranges.
        /// </summary>
        public List<ZoneRange> Zones { get; set; } = new List<ZoneRange>();
    }
}
