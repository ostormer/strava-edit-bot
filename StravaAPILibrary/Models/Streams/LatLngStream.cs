using StravaAPILibary.Models.Streams;
using StravaAPILibary.Models.Common;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of GPS coordinates (latitude/longitude).
    /// </summary>
    public class LatLngStream : BaseStream
    {
        /// <summary>
        /// The sequence of latitude/longitude coordinate pairs.
        /// </summary>
        public List<LatLng> Data { get; set; } = new();
    }
}
