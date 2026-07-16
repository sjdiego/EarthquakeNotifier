using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EarthquakeNotifier.Common;
using EarthquakeNotifier.Domain;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Api.SeismicPortal
{
    /// <summary>
    /// Implementation of IEarthquakeApiClient for SeismicPortal FDSN API.
    /// Consumes JSON-formatted earthquake data from SeismicPortal European earthquake monitoring service.
    /// </summary>
    public partial class SeismicPortalEarthquakeApiClient(
        HttpClient httpClient,
        ILogger<SeismicPortalEarthquakeApiClient> logger) : IEarthquakeApiClient
    {
        private const string ApiUrl = "https://www.seismicportal.eu/fdsnws/event/1/query";
        private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Retrieves recent earthquake events from SeismicPortal API filtered by minimum magnitude.
        /// Returns a failure result if the API is unreachable or the response cannot be parsed.
        /// </summary>
        /// <param name="minMagnitude">Minimum earthquake magnitude to include in results.</param>
        public async Task<Result<List<EarthquakeNotification>>> GetRecentEarthquakesAsync(double minMagnitude)
        {
            try
            {
                LogFetchingEarthquakes(logger, minMagnitude);

                var requestUrl = BuildRequestUrl(minMagnitude);
                var response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                var featureCollection = await JsonSerializer.DeserializeAsync<SeismicPortalFeatureCollection>(stream, _options);

                if (featureCollection?.Features == null)
                {
                    LogNoFeatures(logger);
                    return Result<List<EarthquakeNotification>>.Success([]);
                }

                var earthquakes = featureCollection.Features
                    .Where(f => f.Properties.Magnitude.HasValue && f.Properties.Magnitude.Value >= minMagnitude)
                    .Select(MapToEarthquakeNotification)
                    .Where(e => e != null)
                    .Cast<EarthquakeNotification>()
                    .OrderBy(e => e.Time)
                    .ToList();

                LogRetrievedCount(logger, earthquakes.Count);
                return Result<List<EarthquakeNotification>>.Success(earthquakes);
            }
            catch (HttpRequestException ex)
            {
                LogHttpError(logger, ex);
                return Result<List<EarthquakeNotification>>.Failure("SeismicPortal API request failed", ex);
            }
            catch (JsonException ex)
            {
                LogJsonError(logger, ex);
                return Result<List<EarthquakeNotification>>.Failure("Failed to parse SeismicPortal API response", ex);
            }
            catch (Exception ex)
            {
                LogUnexpectedError(logger, ex);
                return Result<List<EarthquakeNotification>>.Failure("Unexpected error fetching SeismicPortal earthquakes", ex);
            }
        }

        /// <summary>
        /// Builds the request URL with query parameters for minimum magnitude.
        /// </summary>
        private static string BuildRequestUrl(double minMagnitude)
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
                LogMappingError(logger, ex);
                return null;
            }
        }

        /// <summary>
        /// Parses SeismicPortal ISO 8601 datetime string to DateTime.
        /// </summary>
        private DateTime ParseSeismicPortalDateTime(string dateTimeString)
        {
            if (DateTime.TryParse(dateTimeString, out var dateTime))
                return dateTime;

            LogDateTimeParseWarning(logger, dateTimeString);
            return DateTime.UtcNow;
        }
    }
}
