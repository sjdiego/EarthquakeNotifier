using System;

namespace EarthquakeNotifier.Domain
{
    /// <summary>
    /// Unified earthquake notification model used across both USGS and SeismicPortal APIs.
    /// This model serves as the common data transfer object for webhook formatters and storage operations.
    /// </summary>
    public class EarthquakeNotification
    {
        /// <summary>
        /// Unique identifier for the earthquake event.
        /// </summary>
        public required string EarthquakeId { get; set; }

        /// <summary>
        /// Magnitude of the earthquake.
        /// </summary>
        public required double Magnitude { get; set; }

        /// <summary>
        /// Human-readable description of the earthquake location.
        /// </summary>
        public required string Place { get; set; }

        /// <summary>
        /// Time of the earthquake occurrence in UTC.
        /// </summary>
        public required DateTime Time { get; set; }

        /// <summary>
        /// Latitude of the epicenter.
        /// </summary>
        public required double Latitude { get; set; }

        /// <summary>
        /// Longitude of the epicenter.
        /// </summary>
        public required double Longitude { get; set; }

        /// <summary>
        /// Depth of the earthquake hypocenter in kilometers.
        /// </summary>
        public required double Depth { get; set; }

        /// <summary>
        /// URL to the original earthquake event information.
        /// </summary>
        public required string Url { get; set; }
    }
}
