using StravaAPILibrary.Models.Streams;
using StravaAPILibrary.Models.Common;

namespace StravaAPILibrary.Models.Streams;

/// <summary>
/// Represents a stream of GPS coordinates (latitude/longitude).
/// </summary>
public class LatLngStream : BaseStream
{
    /// <summary>
    /// The sequence of latitude/longitude coordinate pairs.
    /// </summary>
    public List<LatLng> Data { get; set; } = new();
}
