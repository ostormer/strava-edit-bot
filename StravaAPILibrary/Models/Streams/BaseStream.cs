namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents the base properties of a stream returned by the Strava API.
    /// </summary>
    public class BaseStream
    {
        /// <summary>
        /// The number of data points in this stream.
        /// </summary>
        public int OriginalSize { get; set; }

        /// <summary>
        /// The level of detail (sampling) in which this stream was returned.
        /// Possible values: low, medium, high.
        /// </summary>
        public string Resolution { get; set; } = string.Empty;

        /// <summary>
        /// The base series used in the case the stream was downsampled.
        /// Possible values: distance, time.
        /// </summary>
        public string SeriesType { get; set; } = string.Empty;
    }
}
