using StravaAPILibary.Models.Streams;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of heart rate data points.
    /// </summary>
    public class HeartrateStream : BaseStream
    {
        /// <summary>
        /// The sequence of heart rate values, in beats per minute.
        /// </summary>
        public List<int> Data { get; set; } = new();
    }
}
