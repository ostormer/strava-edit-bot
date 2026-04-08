using StravaAPILibary.Models.Streams;

namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a stream of temperature data points.
    /// </summary>
    public class TemperatureStream : BaseStream
    {
        /// <summary>
        /// The sequence of temperature values, in degrees Celsius.
        /// </summary>
        public List<int> Data { get; set; } = new();
    }
}
