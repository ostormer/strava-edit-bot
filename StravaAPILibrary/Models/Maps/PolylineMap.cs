namespace StravaAPILibary.Models.Maps
{
    /// <summary>
    /// Represents the polyline map data of an activity, route, or segment.
    /// </summary>
    public class PolylineMap
    {
        /// <summary>
        /// The identifier of the map.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The polyline of the map, only returned in detailed representations.
        /// </summary>
        public string? Polyline { get; set; }

        /// <summary>
        /// The summary polyline of the map.
        /// </summary>
        public string? SummaryPolyline { get; set; }
    }
}
