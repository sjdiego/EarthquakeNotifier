using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EarthquakeNotifier.Infrastructure.Api.Usgs
{
    /// <summary>
    /// USGS GeoJSON models for earthquake data deserialization.
    /// These classes mirror the GeoJSON structure returned by USGS Earthquake Hazards Program API.
    /// </summary>

    public class UsgsFeatureCollection
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("features")]
        public List<UsgsFeature> Features { get; set; } = new();
    }

    public class UsgsFeature
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        public UsgsProperties Properties { get; set; } = new();

        [JsonPropertyName("geometry")]
        public UsgsGeometry Geometry { get; set; } = new();
    }

    public class UsgsProperties
    {
        [JsonPropertyName("mag")]
        public double? Magnitude { get; set; }

        [JsonPropertyName("place")]
        public string Place { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class UsgsGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("coordinates")]
        public List<double> Coordinates { get; set; } = new();
    }
}
