using StravaAPILibary.Models.Streams;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of distance data points.
    /// </summary>
    public class DistanceStream : BaseStream
    {
        /// <summary>
        /// The sequence of distance values, in meters.
        /// </summary>
        public List<float> Data { get; set; } = new();
    }
}
