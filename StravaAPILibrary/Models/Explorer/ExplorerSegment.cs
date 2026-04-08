using StravaAPILibary.Models.Common;

namespace StravaAPILibary.Models.Explorer
{
    /// <summary>
    /// Represents a segment returned from the Explorer API.
    /// </summary>
    public class ExplorerSegment
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
        /// The category of the climb [0, 5].
        /// Higher is harder (e.g. 5 = Hors catégorie, 0 = uncategorized).
        /// </summary>
        public int ClimbCategory { get; set; }

        /// <summary>
        /// The description for the category of the climb (NC, 4, 3, 2, 1, HC).
        /// </summary>
        public string ClimbCategoryDesc { get; set; } = string.Empty;

        /// <summary>
        /// The segment's average grade in percents.
        /// </summary>
        public float AvgGrade { get; set; }

        /// <summary>
        /// The starting latitude/longitude of the segment.
        /// </summary>
        public LatLng StartLatLng { get; set; } = new LatLng();

        /// <summary>
        /// The ending latitude/longitude of the segment.
        /// </summary>
        public LatLng EndLatLng { get; set; } = new LatLng();

        /// <summary>
        /// The segment's elevation difference in meters.
        /// </summary>
        public float ElevDifference { get; set; }

        /// <summary>
        /// The segment's distance in meters.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The polyline of the segment.
        /// </summary>
        public string Points { get; set; } = string.Empty;
    }
}
