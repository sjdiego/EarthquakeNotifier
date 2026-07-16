using System;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Api.Usgs
{
    public partial class UsgsEarthquakeApiClient
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Fetching earthquakes from USGS API with minimum magnitude: {minMagnitude}")]
        private static partial void LogFetchingEarthquakes(ILogger logger, double minMagnitude);

        [LoggerMessage(Level = LogLevel.Warning, Message = "No features found in USGS API response")]
        private static partial void LogNoFeatures(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully retrieved {count} earthquakes from USGS API")]
        private static partial void LogRetrievedCount(ILogger logger, int count);

        [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error occurred while fetching USGS earthquakes")]
        private static partial void LogHttpError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "JSON deserialization error occurred while parsing USGS response")]
        private static partial void LogJsonError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error occurred while fetching USGS earthquakes")]
        private static partial void LogUnexpectedError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Error mapping USGS feature to EarthquakeNotification")]
        private static partial void LogMappingError(ILogger logger, Exception ex);
    }
}
