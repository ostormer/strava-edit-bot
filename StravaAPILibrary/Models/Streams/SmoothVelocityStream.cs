using StravaAPILibary.Models.Streams;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of smoothed velocity (speed) data points.
    /// </summary>
    public class SmoothVelocityStream : BaseStream
    {
        /// <summary>
        /// The sequence of velocity values, in meters per second.
        /// </summary>
        public List<float> Data { get; set; } = new();
    }
}
