using StravaAPILibary.Models.Streams;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of time data points.
    /// </summary>
    public class TimeStream : BaseStream
    {
        /// <summary>
        /// The sequence of time values, in seconds.
        /// </summary>
        public List<int> Data { get; set; } = new();
    }
}
