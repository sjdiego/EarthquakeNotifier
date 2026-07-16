using System;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier
{
    public partial class EarthquakeMonitorFunction
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Earthquake Notifier function started at: {executionTime}")]
        private static partial void LogFunctionStarted(ILogger logger, DateTime executionTime);

        [LoggerMessage(Level = LogLevel.Information, Message = "Next timer schedule: {nextSchedule}")]
        private static partial void LogNextSchedule(ILogger logger, DateTime nextSchedule);

        [LoggerMessage(Level = LogLevel.Information, Message = "Fetching recent earthquakes with minimum magnitude: {minimumMagnitude}")]
        private static partial void LogFetchingEarthquakes(ILogger logger, double minimumMagnitude);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to fetch earthquakes: {error}")]
        private static partial void LogFetchFailed(ILogger logger, Exception? ex, string? error);

        [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {count} earthquakes matching criteria")]
        private static partial void LogRetrievedCount(ILogger logger, int count);

        [LoggerMessage(Level = LogLevel.Information, Message = "Earthquake processing completed. Total events processed: {count}")]
        private static partial void LogProcessingCompleted(ILogger logger, int count);

        [LoggerMessage(Level = LogLevel.Error, Message = "Fatal error in earthquake notifier function")]
        private static partial void LogFatalError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Information, Message = "Processing earthquake: {earthquakeId} (M{magnitude})")]
        private static partial void LogProcessingEarthquake(ILogger logger, string earthquakeId, double magnitude);

        [LoggerMessage(Level = LogLevel.Information, Message = "Earthquake {earthquakeId} already processed, skipping")]
        private static partial void LogEarthquakeSkipped(ILogger logger, string earthquakeId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully processed earthquake {earthquakeId}")]
        private static partial void LogEarthquakeProcessed(ILogger logger, string earthquakeId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error processing earthquake {earthquakeId}")]
        private static partial void LogProcessingError(ILogger logger, Exception ex, string earthquakeId);
    }
}
