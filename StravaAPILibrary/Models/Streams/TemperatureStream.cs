using StravaAPILibrary.Models.Streams;

namespace StravaAPILibrary.Models.Streams;

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
