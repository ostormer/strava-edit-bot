using StravaAPILibrary.Models.Streams;

namespace StravaAPILibrary.Models.Streams;

/// <summary>
/// Represents a stream of altitude data points.
/// </summary>
public class AltitudeStream : BaseStream
{
    /// <summary>
    /// The sequence of altitude values, in meters.
    /// </summary>
    public List<float> Data { get; set; } = new();
}
