using StravaAPILibary.Models.Streams;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of cadence data points.
    /// </summary>
    public class CadenceStream : BaseStream
    {
        /// <summary>
        /// The sequence of cadence values, in rotations per minute.
        /// </summary>
        public List<int> Data { get; set; } = new();
    }
}
