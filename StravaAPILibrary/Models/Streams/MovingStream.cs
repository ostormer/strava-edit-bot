using StravaAPILibrary.Models.Streams;

namespace StravaAPILibrary.Models.Streams;

/// <summary>
/// Represents a stream indicating whether the athlete was moving.
/// </summary>
public class MovingStream : BaseStream
{
    /// <summary>
    /// The sequence of movement flags, where true indicates movement.
    /// </summary>
    public List<bool> Data { get; set; } = new();
}
