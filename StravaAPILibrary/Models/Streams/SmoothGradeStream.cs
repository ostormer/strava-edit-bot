using StravaAPILibary.Models.Streams;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of smoothed gradient data points.
    /// </summary>
    public class SmoothGradeStream : BaseStream
    {
        /// <summary>
        /// The sequence of gradient values, as percentages.
        /// </summary>
        public List<float> Data { get; set; } = new();
    }
}
