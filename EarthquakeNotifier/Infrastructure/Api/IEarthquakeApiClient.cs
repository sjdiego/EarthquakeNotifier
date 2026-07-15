using System.Collections.Generic;
using System.Threading.Tasks;
using EarthquakeNotifier.Common;
using EarthquakeNotifier.Domain;

namespace EarthquakeNotifier.Infrastructure.Api;

/// <summary>
/// Abstraction for earthquake data provider clients.
/// Supports multiple providers (USGS, SeismicPortal, etc.) behind a common interface.
/// </summary>
public interface IEarthquakeApiClient
{
    /// <summary>
    /// Retrieves recent earthquake events from the API, filtered by minimum magnitude.
    /// Returns a failure result if the API is unreachable or returns invalid data.
    /// </summary>
    /// <param name="minMagnitude">Minimum earthquake magnitude to include in results.</param>
    Task<Result<List<EarthquakeNotification>>> GetRecentEarthquakesAsync(double minMagnitude);
}
