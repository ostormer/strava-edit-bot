using StravaAPILibary.Models.Common;

namespace StravaAPILibary.Models.Routes
{
    /// <summary>
    /// Represents a waypoint along a route.
    /// </summary>
    public class Waypoint
    {
        /// <summary>
        /// The location along the route that the waypoint is closest to.
        /// </summary>
        public LatLng LatLng { get; set; } = new LatLng();

        /// <summary>
        /// A location off of the route that the waypoint is (optional).
        /// </summary>
        public LatLng? TargetLatLng { get; set; }

        /// <summary>
        /// Categories that the waypoint belongs to.
        /// </summary>
        public string Categories { get; set; } = string.Empty;

        /// <summary>
        /// A title for the waypoint.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// A description of the waypoint (optional).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The number of meters along the route where the waypoint is located.
        /// </summary>
        public int DistanceIntoRoute { get; set; }
    }
}
