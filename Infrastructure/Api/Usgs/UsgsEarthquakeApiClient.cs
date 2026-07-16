using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EarthquakeNotifier.Common;
using EarthquakeNotifier.Domain;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Api.Usgs
{
    /// <summary>
    /// Implementation of IEarthquakeApiClient for USGS Earthquake Hazards Program API.
    /// Consumes GeoJSON-formatted earthquake data from USGS endpoints.
    /// </summary>
    public partial class UsgsEarthquakeApiClient(
        HttpClient httpClient,
        ILogger<UsgsEarthquakeApiClient> logger) : IEarthquakeApiClient
    {
        private const string ApiUrl = "https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_hour.geojson";
        private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Retrieves recent earthquake events from USGS API filtered by minimum magnitude.
        /// Returns a failure result if the API is unreachable or the response cannot be parsed.
        /// </summary>
        /// <param name="minMagnitude">Minimum earthquake magnitude to include in results.</param>
        public async Task<Result<List<EarthquakeNotification>>> GetRecentEarthquakesAsync(double minMagnitude)
        {
            try
            {
                LogFetchingEarthquakes(logger, minMagnitude);

                var response = await httpClient.GetAsync(ApiUrl);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                var featureCollection = await JsonSerializer.DeserializeAsync<UsgsFeatureCollection>(stream, _options);

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
                return Result<List<EarthquakeNotification>>.Failure("USGS API request failed", ex);
            }
            catch (JsonException ex)
            {
                LogJsonError(logger, ex);
                return Result<List<EarthquakeNotification>>.Failure("Failed to parse USGS API response", ex);
            }
            catch (Exception ex)
            {
                LogUnexpectedError(logger, ex);
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

                return new EarthquakeNotification
                {
                    EarthquakeId = feature.Id,
                    Magnitude = feature.Properties.Magnitude ?? 0.0,
                    Place = feature.Properties.Place,
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(feature.Properties.Time).UtcDateTime,
                    Latitude = latitude,
                    Longitude = longitude,
                    Depth = depth,
                    Url = feature.Properties.Url
                };
            }
            catch (Exception ex)
            {
                LogMappingError(logger, ex);
                return null;
            }
        }
    }
}
