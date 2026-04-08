namespace StravaAPILibary.Models.Common
{
    /// <summary>
    /// Represents a geographical coordinate as a pair of latitude and longitude.
    /// </summary>
    public class LatLng
    {
        /// <summary>
        /// Latitude component of the coordinate.
        /// </summary>
        public float Latitude { get; set; }

        /// <summary>
        /// Longitude component of the coordinate.
        /// </summary>
        public float Longitude { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="LatLng"/>.
        /// </summary>
        public LatLng() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LatLng"/> with specified latitude and longitude.
        /// </summary>
        /// <param name="latitude">The latitude component.</param>
        /// <param name="longitude">The longitude component.</param>
        public LatLng(float latitude, float longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// Returns the coordinate as an array of floats [latitude, longitude].
        /// </summary>
        public float[] ToArray() => new float[] { Latitude, Longitude };
    }
}
