using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EarthquakeNotifier.Common;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Api;
using EarthquakeNotifier.Infrastructure.Api.SeismicPortal;

namespace EarthquakeNotifier.Infrastructure.Api.SeismicPortal
{
    /// <summary>
    /// Implementation of IEarthquakeApiClient for SeismicPortal FDSN API.
    /// Consumes JSON-formatted earthquake data from SeismicPortal European earthquake monitoring service.
    /// </summary>
    public class SeismicPortalEarthquakeApiClient : IEarthquakeApiClient
    {
        private const string ApiUrl = "https://www.seismicportal.eu/fdsnws/event/1/query";

        private readonly HttpClient _httpClient;
        private readonly ILogger<SeismicPortalEarthquakeApiClient> _logger;

        /// <summary>
        /// Initializes the client with an <see cref="HttpClient"/> configured by <c>IHttpClientFactory</c>.
        /// </summary>
        public SeismicPortalEarthquakeApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SeismicPortalEarthquakeApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves recent earthquake events from SeismicPortal API filtered by minimum magnitude.
        /// Returns a failure result if the API is unreachable or the response cannot be parsed.
        /// </summary>
        /// <param name="minMagnitude">Minimum earthquake magnitude to include in results.</param>
        public async Task<Result<List<EarthquakeNotification>>> GetRecentEarthquakesAsync(double minMagnitude)
        {
            try
            {
                _logger.LogInformation("Fetching earthquakes from SeismicPortal API with minimum magnitude: {MinMagnitude}", minMagnitude);

                var requestUrl = BuildRequestUrl(minMagnitude);
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var featureCollection = JsonSerializer.Deserialize<SeismicPortalFeatureCollection>(content, options);

                if (featureCollection?.Features == null)
                {
                    _logger.LogWarning("No features found in SeismicPortal API response");
                    return Result<List<EarthquakeNotification>>.Success(new List<EarthquakeNotification>());
                }

                var earthquakes = featureCollection.Features
                    .Where(f => f.Properties.Magnitude.HasValue && f.Properties.Magnitude.Value >= minMagnitude)
                    .Select(f => MapToEarthquakeNotification(f))
                    .Where(e => e != null)
                    .Cast<EarthquakeNotification>()
                    .OrderBy(e => e.Time)
                    .ToList();

                _logger.LogInformation("Successfully retrieved {Count} earthquakes from SeismicPortal API", earthquakes.Count);
                return Result<List<EarthquakeNotification>>.Success(earthquakes);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while fetching SeismicPortal earthquakes");
                return Result<List<EarthquakeNotification>>.Failure("SeismicPortal API request failed", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while parsing SeismicPortal response");
                return Result<List<EarthquakeNotification>>.Failure("Failed to parse SeismicPortal API response", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching SeismicPortal earthquakes");
                return Result<List<EarthquakeNotification>>.Failure("Unexpected error fetching SeismicPortal earthquakes", ex);
            }
        }

        /// <summary>
        /// Builds the request URL with query parameters for minimum magnitude.
        /// </summary>
        private string BuildRequestUrl(double minMagnitude)
        {
            var separator = ApiUrl.Contains("?") ? "&" : "?";
            return $"{ApiUrl}{separator}limit=10&minmag={minMagnitude.ToString("F1", CultureInfo.InvariantCulture)}&format=json";
        }

        /// <summary>
        /// Maps a SeismicPortal FDSN JSON feature to a unified EarthquakeNotification model.
        /// </summary>
        private EarthquakeNotification? MapToEarthquakeNotification(SeismicPortalFeature feature)
        {
            try
            {
                if (feature?.Properties == null)
                    return null;

                // Prefer geometry coordinates; fall back to properties lat/lon
                var longitude = feature.Geometry?.Coordinates?.Count >= 2
                    ? feature.Geometry.Coordinates[0]
                    : feature.Properties.Lon ?? 0.0;

                var latitude = feature.Geometry?.Coordinates?.Count >= 2
                    ? feature.Geometry.Coordinates[1]
                    : feature.Properties.Lat ?? 0.0;

                // Depth is in properties; geometry coordinates[2] is negative in SeismicPortal
                var depth = feature.Properties.Depth;

                // Prefer event time over lastupdate
                var timeString = string.IsNullOrEmpty(feature.Properties.Time)
                    ? feature.Properties.LastUpdate
                    : feature.Properties.Time;
                var dateTime = ParseSeismicPortalDateTime(timeString);

                var eventUrl = $"https://www.seismicportal.eu/eventdetails.html?unid={feature.Properties.OriginId}";

                return new EarthquakeNotification
                {
                    EarthquakeId = feature.Properties.OriginId,
                    Magnitude = feature.Properties.Magnitude ?? 0.0,
                    Place = feature.Properties.FlynnRegion,
                    Time = dateTime,
                    Latitude = latitude,
                    Longitude = longitude,
                    Depth = depth,
                    Url = eventUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapping SeismicPortal feature to EarthquakeNotification");
                return null;
            }
        }

        /// <summary>
        /// Parses SeismicPortal ISO 8601 datetime string to DateTime.
        /// </summary>
        private DateTime ParseSeismicPortalDateTime(string dateTimeString)
        {
            if (DateTime.TryParse(dateTimeString, out var dateTime))
            {
                return dateTime;
            }

            _logger.LogWarning("Failed to parse SeismicPortal datetime: {DateTimeString}", dateTimeString);
            return DateTime.UtcNow;
        }
    }
}
