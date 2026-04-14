using StravaAPILibrary.Models.Streams;

namespace StravaAPILibrary.Models.Streams;

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
