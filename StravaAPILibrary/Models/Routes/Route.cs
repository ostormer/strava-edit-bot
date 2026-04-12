using StravaAPILibary.Models.Common;
using StravaAPILibary.Models.Maps;
using StravaAPILibary.Models.Segments;
using StravaAPILibary.Models.Athletes;
using StravaAPILibary.Models.Routes;

namespace StravaAPILibary.Models.Routes
{
    /// <summary>
    /// Represents a Strava route, including its metadata and associated details.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// The athlete who created the route.
        /// </summary>
        public SummaryAthlete Athlete { get; set; } = new SummaryAthlete();

        /// <summary>
        /// The description of the route.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The distance of the route, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The elevation gain of the route, in meters.
        /// </summary>
        public float ElevationGain { get; set; }

        /// <summary>
        /// The unique identifier of this route.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The unique identifier of this route in string format.
        /// </summary>
        public string IdStr { get; set; } = string.Empty;

        /// <summary>
        /// The polyline map of the route.
        /// </summary>
        public PolylineMap Map { get; set; } = new PolylineMap();

        /// <summary>
        /// The name of the route.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether this route is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// Whether this route is starred by the logged-in athlete.
        /// </summary>
        public bool Starred { get; set; }

        /// <summary>
        /// An epoch timestamp of when the route was created.
        /// </summary>
        public int Timestamp { get; set; }

        /// <summary>
        /// This route's type (1 for ride, 2 for run).
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// This route's sub-type (1 for road, 2 for mountain bike, 3 for cross, 4 for trail, 5 for mixed).
        /// </summary>
        public int SubType { get; set; }

        /// <summary>
        /// The time at which the route was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The time at which the route was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Estimated time in seconds for the authenticated athlete to complete the route.
        /// </summary>
        public int EstimatedMovingTime { get; set; }

        /// <summary>
        /// The segments traversed by this route.
        /// </summary>
        public List<SummarySegment> Segments { get; set; } = new List<SummarySegment>();

        /// <summary>
        /// The custom waypoints along this route.
        /// </summary>
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();
    }
}
