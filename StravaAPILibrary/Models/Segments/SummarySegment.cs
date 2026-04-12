using StravaAPILibary.Models.Common;
using StravaAPILibary.Models.Segments;

namespace StravaAPILibary.Models.Segments
{
    /// <summary>
    /// Represents a summary of a Strava segment.
    /// </summary>
    public class SummarySegment
    {
        /// <summary>
        /// The unique identifier of this segment.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The name of this segment.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The activity type of the segment (e.g. Ride, Run).
        /// </summary>
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>
        /// The distance of the segment, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The segment's average grade, in percent.
        /// </summary>
        public float AverageGrade { get; set; }

        /// <summary>
        /// The segment's maximum grade, in percent.
        /// </summary>
        public float MaximumGrade { get; set; }

        /// <summary>
        /// The segment's highest elevation, in meters.
        /// </summary>
        public float ElevationHigh { get; set; }

        /// <summary>
        /// The segment's lowest elevation, in meters.
        /// </summary>
        public float ElevationLow { get; set; }

        /// <summary>
        /// The starting latitude/longitude of the segment.
        /// </summary>
        public LatLng StartLatLng { get; set; } = new LatLng();

        /// <summary>
        /// The ending latitude/longitude of the segment.
        /// </summary>
        public LatLng EndLatLng { get; set; } = new LatLng();

        /// <summary>
        /// The climb category of the segment (0-5).
        /// </summary>
        public int ClimbCategory { get; set; }

        /// <summary>
        /// The city where the segment is located.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// The state or geographical region where the segment is located.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// The country where the segment is located.
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the segment is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// The personal record (PR) effort for this segment.
        /// </summary>
        public SummaryPRSegmentEffort AthletePrEffort { get; set; } = new SummaryPRSegmentEffort();

        /// <summary>
        /// The athlete's segment statistics.
        /// </summary>
        public SummarySegmentEffort AthleteSegmentStats { get; set; } = new SummarySegmentEffort();
    }
}
