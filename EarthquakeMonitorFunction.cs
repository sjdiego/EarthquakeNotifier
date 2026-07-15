using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Api;
using EarthquakeNotifier.Infrastructure.Notifications;
using EarthquakeNotifier.Infrastructure.Storage;
using EarthquakeNotifier.Telemetry;

namespace EarthquakeNotifier
{
    /// <summary>
    /// Azure Timer-triggered Function that monitors global seismic activity and dispatches webhook notifications.
    /// Orchestrates the full pipeline: fetch → deduplicate → notify → track metrics.
    /// The trigger schedule is controlled by the <c>EARTHQUAKE_SCHEDULE</c> app setting (cron expression).
    /// </summary>
    public class EarthquakeMonitorFunction
    {
        private readonly ILogger<EarthquakeMonitorFunction> _logger;
        private readonly IEarthquakeApiClient _earthquakeApiClient;
        private readonly IEarthquakeStorageService _storageService;
        private readonly IWebhookNotificationService _webhookService;
        private readonly IConfiguration _configuration;
        private readonly double _minimumMagnitude;
        private readonly EarthquakeMetrics _metrics;

        /// <summary>
        /// Initializes the function with all required services.
        /// Resolves <c>MIN_EARTHQUAKE_MAGNITUDE</c> once at startup (defaults to 4.0 if missing or invalid).
        /// </summary>
        public EarthquakeMonitorFunction(
            ILogger<EarthquakeMonitorFunction> logger,
            IEarthquakeApiClient earthquakeApiClient,
            IEarthquakeStorageService storageService,
            IWebhookNotificationService webhookService,
            IConfiguration configuration,
            EarthquakeMetrics metrics)
        {
            _logger              = logger;
            _earthquakeApiClient = earthquakeApiClient;
            _storageService      = storageService;
            _webhookService      = webhookService;
            _configuration       = configuration;
            _minimumMagnitude    = ParseMagnitude(configuration["MIN_EARTHQUAKE_MAGNITUDE"], fallback: 4.0);
            _metrics             = metrics;
        }

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
                _logger.LogInformation("Earthquake Notifier function started at: {executionTime}", DateTime.Now);
                if (myTimer.ScheduleStatus is not null)
                    _logger.LogInformation("Next timer schedule: {nextSchedule}", myTimer.ScheduleStatus.Next);

                _logger.LogInformation("Fetching recent earthquakes with minimum magnitude: {minimumMagnitude}", _minimumMagnitude);
                var result = await _earthquakeApiClient.GetRecentEarthquakesAsync(_minimumMagnitude);

                if (!result.IsSuccess)
                {
                    _logger.LogError(result.Exception, "Failed to fetch earthquakes: {error}", result.ErrorMessage);
                    _metrics.TrackApiFailed(
                        provider: _configuration["EARTHQUAKE_API_PROVIDER"] ?? "usgs",
                        errorMessage: result.ErrorMessage ?? "Unknown error",
                        exception: result.Exception);
                    return;
                }

                var earthquakes = result.Value!;
                _logger.LogInformation("Retrieved {count} earthquakes matching criteria", earthquakes.Count);

                foreach (var earthquake in earthquakes)
                {
                    await ProcessEarthquakeEventAsync(earthquake);
                    await Task.Delay(100);
                }

                _logger.LogInformation("Earthquake processing completed. Total events processed: {count}", earthquakes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in earthquake notifier function");
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
                _logger.LogInformation("Processing earthquake: {earthquakeId} (M{magnitude})", earthquake.EarthquakeId, earthquake.Magnitude);

                var saved = await _storageService.TrySaveAsync(earthquake);
                if (!saved)
                {
                    _logger.LogInformation("Earthquake {earthquakeId} already processed, skipping", earthquake.EarthquakeId);
                    return;
                }

                await _webhookService.SendAsync(earthquake);

                _metrics.TrackEarthquakeProcessed(earthquake);
                _logger.LogInformation("Successfully processed earthquake {earthquakeId}", earthquake.EarthquakeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing earthquake {earthquakeId}", earthquake.EarthquakeId);
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
