using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EarthquakeNotifier.Common;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Api;
using EarthquakeNotifier.Infrastructure.Api.Usgs;

namespace EarthquakeNotifier.Infrastructure.Api.Usgs
{
    /// <summary>
    /// Implementation of IEarthquakeApiClient for USGS Earthquake Hazards Program API.
    /// Consumes GeoJSON-formatted earthquake data from USGS endpoints.
    /// </summary>
    public class UsgsEarthquakeApiClient : IEarthquakeApiClient
    {
        private const string ApiUrl = "https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson";

        private readonly HttpClient _httpClient;
        private readonly ILogger<UsgsEarthquakeApiClient> _logger;

        /// <summary>
        /// Initializes the client with an <see cref="HttpClient"/> configured by <c>IHttpClientFactory</c>.
        /// </summary>
        public UsgsEarthquakeApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<UsgsEarthquakeApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves recent earthquake events from USGS API filtered by minimum magnitude.
        /// Returns a failure result if the API is unreachable or the response cannot be parsed.
        /// </summary>
        /// <param name="minMagnitude">Minimum earthquake magnitude to include in results.</param>
        public async Task<Result<List<EarthquakeNotification>>> GetRecentEarthquakesAsync(double minMagnitude)
        {
            try
            {
                _logger.LogInformation("Fetching earthquakes from USGS API with minimum magnitude: {MinMagnitude}", minMagnitude);

                var response = await _httpClient.GetAsync(ApiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var featureCollection = JsonSerializer.Deserialize<UsgsFeatureCollection>(content, options);

                if (featureCollection?.Features == null)
                {
                    _logger.LogWarning("No features found in USGS API response");
                    return Result<List<EarthquakeNotification>>.Success(new List<EarthquakeNotification>());
                }

                var earthquakes = featureCollection.Features
                    .Where(f => f.Properties.Magnitude.HasValue && f.Properties.Magnitude.Value >= minMagnitude)
                    .Select(f => MapToEarthquakeNotification(f))
                    .Where(e => e != null)
                    .Cast<EarthquakeNotification>()
                    .OrderBy(e => e.Time)
                    .ToList();

                _logger.LogInformation("Successfully retrieved {Count} earthquakes from USGS API", earthquakes.Count);
                return Result<List<EarthquakeNotification>>.Success(earthquakes);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching USGS earthquakes");
                return Result<List<EarthquakeNotification>>.Failure("USGS API request failed", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while parsing USGS response");
                return Result<List<EarthquakeNotification>>.Failure("Failed to parse USGS API response", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching USGS earthquakes");
                return Result<List<EarthquakeNotification>>.Failure("Unexpected error fetching USGS earthquakes", ex);
            }
        }

        /// <summary>
        /// Maps a USGS GeoJSON feature to a unified EarthquakeNotification model.
        /// </summary>
        private EarthquakeNotification? MapToEarthquakeNotification(UsgsFeature feature)
        {
            try
            {
                if (feature?.Properties == null || feature.Geometry?.Coordinates == null)
                    return null;

                if (feature.Geometry.Coordinates.Count < 2)
                    return null;

                // USGS coordinates are [longitude, latitude, depth]
                var longitude = feature.Geometry.Coordinates[0];
                var latitude = feature.Geometry.Coordinates[1];
                var depth = feature.Geometry.Coordinates.Count > 2 ? feature.Geometry.Coordinates[2] : 0.0;

                // Convert Unix timestamp (milliseconds) to DateTime
                var dateTime = UnixTimeStampToDateTime(feature.Properties.Time);

                return new EarthquakeNotification
                {
                    EarthquakeId = feature.Id,
                    Magnitude = feature.Properties.Magnitude ?? 0.0,
                    Place = feature.Properties.Place,
                    Time = dateTime,
                    Latitude = latitude,
                    Longitude = longitude,
                    Depth = depth,
                    Url = feature.Properties.Url
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapping USGS feature to EarthquakeNotification");
                return null;
            }
        }

        /// <summary>
        /// Converts USGS Unix timestamp (milliseconds since epoch) to DateTime.
        /// </summary>
        private DateTime UnixTimeStampToDateTime(long timestamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timestamp);
            return dateTime;
        }
    }
}
