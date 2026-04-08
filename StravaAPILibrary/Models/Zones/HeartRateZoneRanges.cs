namespace StravaAPILibary.Models.Zones
{
    /// <summary>
    /// Represents heart rate zone ranges for an athlete.
    /// </summary>
    public class HeartRateZoneRanges
    {
        /// <summary>
        /// Indicates whether the athlete has set their own custom heart rate zones.
        /// </summary>
        public bool CustomZones { get; set; }

        /// <summary>
        /// The heart rate zones for the athlete.
        /// </summary>
        public ZoneRanges Zones { get; set; } = new ZoneRanges();
    }
}
