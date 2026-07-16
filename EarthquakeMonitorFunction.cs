using System;
using System.Globalization;
using System.Threading.Tasks;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Api;
using EarthquakeNotifier.Infrastructure.Notifications;
using EarthquakeNotifier.Infrastructure.Storage;
using EarthquakeNotifier.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier
{
    /// <summary>
    /// Azure Timer-triggered Function that monitors global seismic activity and dispatches webhook notifications.
    /// Orchestrates the full pipeline: fetch → deduplicate → notify → track metrics.
    /// The trigger schedule is controlled by the <c>EARTHQUAKE_SCHEDULE</c> app setting (cron expression).
    /// </summary>
    /// <remarks>
    /// Initializes the function with all required services.
    /// Resolves <c>MIN_EARTHQUAKE_MAGNITUDE</c> once at startup (defaults to 4.0 if missing or invalid).
    /// </remarks>
    public partial class EarthquakeMonitorFunction(
        ILogger<EarthquakeMonitorFunction> logger,
        IEarthquakeApiClient earthquakeApiClient,
        IEarthquakeStorageService storageService,
        IWebhookNotificationService webhookService,
        IConfiguration configuration,
        EarthquakeMetrics metrics)
    {
        private readonly double _minimumMagnitude = ParseMagnitude(configuration["MIN_EARTHQUAKE_MAGNITUDE"], fallback: 4.0);

        /// <summary>
        /// Entry point invoked by the Azure Functions runtime on each timer tick.
        /// Fetches earthquakes, processes new ones and skips already-seen events.
        /// </summary>
        /// <param name="myTimer">Timer metadata including the next scheduled execution time.</param>
        [Function("EarthquakeMonitor")]
        public async Task Run([TimerTrigger("%EARTHQUAKE_SCHEDULE%")] TimerInfo myTimer)
        {
            try
            {
                LogFunctionStarted(logger, DateTime.Now);
                if (myTimer.ScheduleStatus is not null)
                    LogNextSchedule(logger, myTimer.ScheduleStatus.Next);

                LogFetchingEarthquakes(logger, _minimumMagnitude);
                var result = await earthquakeApiClient.GetRecentEarthquakesAsync(_minimumMagnitude);

                if (!result.IsSuccess)
                {
                    LogFetchFailed(logger, result.Exception, result.ErrorMessage);
                    metrics.TrackApiFailed(
                        provider: configuration["EARTHQUAKE_API_PROVIDER"] ?? "usgs",
                        errorMessage: result.ErrorMessage ?? "Unknown error",
                        exception: result.Exception);
                    return;
                }

                var earthquakes = result.Value!;
                LogRetrievedCount(logger, earthquakes.Count);

                foreach (var earthquake in earthquakes)
                {
                    await ProcessEarthquakeEventAsync(earthquake);
                    await Task.Delay(100);
                }

                LogProcessingCompleted(logger, earthquakes.Count);
            }
            catch (Exception ex)
            {
                LogFatalError(logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Processes a single earthquake event: saves it to blob storage for deduplication,
        /// sends a webhook notification if it is new, and tracks a custom metric.
        /// </summary>
        /// <param name="earthquake">The earthquake event to process.</param>
        private async Task ProcessEarthquakeEventAsync(EarthquakeNotification earthquake)
        {
            try
            {
                LogProcessingEarthquake(logger, earthquake.EarthquakeId, earthquake.Magnitude);

                var saved = await storageService.TrySaveAsync(earthquake);
                if (!saved)
                {
                    LogEarthquakeSkipped(logger, earthquake.EarthquakeId);
                    return;
                }

                await webhookService.SendAsync(earthquake);

                metrics.TrackEarthquakeProcessed(earthquake);
                LogEarthquakeProcessed(logger, earthquake.EarthquakeId);
            }
            catch (Exception ex)
            {
                LogProcessingError(logger, ex, earthquake.EarthquakeId);
            }
        }

        /// <summary>
        /// Parses a magnitude string from configuration, accepting both dot and comma as decimal separator.
        /// Returns <paramref name="fallback"/> if the value is null, empty, or cannot be parsed.
        /// </summary>
        private static double ParseMagnitude(string? value, double fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;
            var normalized = value.Trim().Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;
        }
    }
}
