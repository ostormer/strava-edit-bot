using StravaAPILibary.Models.Common;
using StravaAPILibary.Models.Maps;
using StravaAPILibary.Models.Enums;
using System;

namespace StravaAPILibary.Models.Segments
{
    /// <summary>
    /// Represents detailed information about a Strava segment.
    /// </summary>
    public class DetailedSegment
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
        /// The activity type of this segment (e.g., Ride, Run).
        /// </summary>
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>
        /// The segment's distance, in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The segment's average grade, in percents.
        /// </summary>
        public float AverageGrade { get; set; }

        /// <summary>
        /// The segment's maximum grade, in percents.
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
        /// The segment's start coordinates (latitude/longitude).
        /// </summary>
        public LatLng? StartLatLng { get; set; }

        /// <summary>
        /// The segment's end coordinates (latitude/longitude).
        /// </summary>
        public LatLng? EndLatLng { get; set; }

        /// <summary>
        /// The category of the climb [0, 5]. Higher is harder (e.g., 5 = HC, 0 = uncategorized).
        /// </summary>
        public int ClimbCategory { get; set; }

        /// <summary>
        /// The city where the segment is located.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// The state or geographical region of the segment.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// The country of the segment.
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Whether this segment is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// The athlete's personal record effort on this segment.
        /// </summary>
        public SummaryPRSegmentEffort? AthletePrEffort { get; set; }

        /// <summary>
        /// The athlete's segment statistics.
        /// </summary>
        public SummarySegmentEffort? AthleteSegmentStats { get; set; }

        /// <summary>
        /// The date and time when the segment was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The date and time when the segment was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The total elevation gain of the segment, in meters.
        /// </summary>
        public float TotalElevationGain { get; set; }

        /// <summary>
        /// The polyline map of the segment.
        /// </summary>
        public PolylineMap? Map { get; set; }

        /// <summary>
        /// The total number of efforts recorded for this segment.
        /// </summary>
        public int EffortCount { get; set; }

        /// <summary>
        /// The number of unique athletes who have an effort on this segment.
        /// </summary>
        public int AthleteCount { get; set; }

        /// <summary>
        /// Indicates whether this segment is considered hazardous.
        /// </summary>
        public bool Hazardous { get; set; }

        /// <summary>
        /// The number of stars for this segment.
        /// </summary>
        public int StarCount { get; set; }
    }
}
