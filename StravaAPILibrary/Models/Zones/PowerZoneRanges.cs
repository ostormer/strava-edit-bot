namespace StravaAPILibary.Models.Zones
{
    /// <summary>
    /// Represents the power zone ranges for an athlete.
    /// </summary>
    public class PowerZoneRanges
    {
        /// <summary>
        /// The collection of power zone ranges.
        /// </summary>
        public ZoneRanges Zones { get; set; } = new ZoneRanges();
    }
}
