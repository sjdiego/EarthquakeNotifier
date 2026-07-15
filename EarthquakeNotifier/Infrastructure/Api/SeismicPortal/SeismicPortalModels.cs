using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EarthquakeNotifier.Infrastructure.Api.SeismicPortal
{
    /// <summary>
    /// SeismicPortal FDSN JSON models for earthquake data deserialization.
    /// These classes mirror the JSON structure returned by SeismicPortal API.
    /// </summary>

    public class SeismicPortalFeatureCollection
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("features")]
        public List<SeismicPortalFeature> Features { get; set; } = new();
    }

    public class SeismicPortalFeature
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        public SeismicPortalProperties Properties { get; set; } = new();

        [JsonPropertyName("geometry")]
        public SeismicPortalGeometry Geometry { get; set; } = new();
    }

    public class SeismicPortalProperties
    {
        [JsonPropertyName("mag")]
        public double? Magnitude { get; set; }

        [JsonPropertyName("magtype")]
        public string MagnitudeType { get; set; } = string.Empty;

        [JsonPropertyName("flynn_region")]
        public string FlynnRegion { get; set; } = string.Empty;

        [JsonPropertyName("lastupdate")]
        public string LastUpdate { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("unid")]
        public string OriginId { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("depth")]
        public double Depth { get; set; }
    }

    public class SeismicPortalGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("coordinates")]
        public List<double> Coordinates { get; set; } = new();
    }
}
