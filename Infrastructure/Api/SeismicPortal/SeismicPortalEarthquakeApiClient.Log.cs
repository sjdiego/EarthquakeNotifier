using System;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Api.SeismicPortal
{
    public partial class SeismicPortalEarthquakeApiClient
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Fetching earthquakes from SeismicPortal API with minimum magnitude: {minMagnitude}")]
        private static partial void LogFetchingEarthquakes(ILogger logger, double minMagnitude);

        [LoggerMessage(Level = LogLevel.Warning, Message = "No features found in SeismicPortal API response")]
        private static partial void LogNoFeatures(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully retrieved {count} earthquakes from SeismicPortal API")]
        private static partial void LogRetrievedCount(ILogger logger, int count);

        [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error occurred while fetching SeismicPortal earthquakes")]
        private static partial void LogHttpError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "JSON deserialization error occurred while parsing SeismicPortal response")]
        private static partial void LogJsonError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error occurred while fetching SeismicPortal earthquakes")]
        private static partial void LogUnexpectedError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Error mapping SeismicPortal feature to EarthquakeNotification")]
        private static partial void LogMappingError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse SeismicPortal datetime: {dateTimeString}")]
        private static partial void LogDateTimeParseWarning(ILogger logger, string dateTimeString);
    }
}
