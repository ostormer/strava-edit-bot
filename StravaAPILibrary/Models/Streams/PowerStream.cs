using StravaAPILibrary.Models.Streams;

namespace StravaAPILibrary.Models.Streams;

/// <summary>
/// Represents a stream of power data points.
/// </summary>
public class PowerStream : BaseStream
{
    /// <summary>
    /// The sequence of power values, in watts.
    /// </summary>
    public List<int> Data { get; set; } = new();
}
