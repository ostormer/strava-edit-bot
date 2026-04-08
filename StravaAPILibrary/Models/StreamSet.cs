namespace StravaAPILibary.Models.Streams
{
    /// <summary>
    /// Represents a collection of different data streams associated with an activity or segment.
    /// </summary>
    public class StreamSet
    {
        /// <summary>
        /// Stream of time values (seconds).
        /// </summary>
        public TimeStream? Time { get; set; }

        /// <summary>
        /// Stream of distance values (meters).
        /// </summary>
        public DistanceStream? Distance { get; set; }

        /// <summary>
        /// Stream of GPS coordinates (latitude/longitude pairs).
        /// </summary>
        public LatLngStream? LatLng { get; set; }

        /// <summary>
        /// Stream of altitude values (meters).
        /// </summary>
        public AltitudeStream? Altitude { get; set; }

        /// <summary>
        /// Stream of smoothed velocity values (m/s).
        /// </summary>
        public SmoothVelocityStream? VelocitySmooth { get; set; }

        /// <summary>
        /// Stream of heart rate values (bpm).
        /// </summary>
        public HeartrateStream? Heartrate { get; set; }

        /// <summary>
        /// Stream of cadence values (rotations per minute).
        /// </summary>
        public CadenceStream? Cadence { get; set; }

        /// <summary>
        /// Stream of power values (watts).
        /// </summary>
        public PowerStream? Watts { get; set; }

        /// <summary>
        /// Stream of temperature values (°C).
        /// </summary>
        public TemperatureStream? Temp { get; set; }

        /// <summary>
        /// Stream indicating movement state (true = moving).
        /// </summary>
        public MovingStream? Moving { get; set; }

        /// <summary>
        /// Stream of smoothed gradient values (percent grade).
        /// </summary>
        public SmoothGradeStream? GradeSmooth { get; set; }
    }
}
