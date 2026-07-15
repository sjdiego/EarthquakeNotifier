using System;
using System.Collections.Generic;
using EarthquakeNotifier.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace EarthquakeNotifier.Telemetry;

/// <summary>
/// Tracks Application Insights custom telemetry for earthquake processing events.
/// </summary>
public class EarthquakeMetrics
{
    private readonly TelemetryClient _telemetryClient;

    private const string EventEarthquakeProcessed = "EarthquakeProcessed";
    private const string EventApiFailed           = "EarthquakeApiFailed";
    private const string EventWebhookFailed       = "WebhookFailed";

    public EarthquakeMetrics(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    /// <summary>
    /// Tracks a successfully processed earthquake event with its key properties.
    /// </summary>
    public void TrackEarthquakeProcessed(EarthquakeNotification earthquake)
    {
        _telemetryClient.TrackEvent(EventEarthquakeProcessed, new Dictionary<string, string>
        {
            ["earthquakeId"] = earthquake.EarthquakeId,
            ["place"]        = earthquake.Place,
            ["magnitude"]    = earthquake.Magnitude.ToString("F1"),
            ["depth"]        = earthquake.Depth.ToString("F1"),
            ["time"]         = earthquake.Time.ToString("O"),
            ["url"]          = earthquake.Url
        });
    }

    /// <summary>
    /// Tracks a failed attempt to retrieve earthquake data from the configured API provider.
    /// </summary>
    public void TrackApiFailed(string provider, string errorMessage, Exception? exception = null)
    {
        if (exception is not null)
        {
            var exTelemetry = new ExceptionTelemetry(exception);
            exTelemetry.Properties["provider"] = provider;
            exTelemetry.Properties["context"]  = EventApiFailed;
            _telemetryClient.TrackException(exTelemetry);
        }

        _telemetryClient.TrackEvent(EventApiFailed, new Dictionary<string, string>
        {
            ["provider"]     = provider,
            ["errorMessage"] = errorMessage
        });
    }

    /// <summary>
    /// Tracks a failed webhook notification, including the HTTP status code when available.
    /// </summary>
    public void TrackWebhookFailed(string earthquakeId, int? httpStatusCode = null, Exception? exception = null)
    {
        if (exception is not null)
        {
            var exTelemetry = new ExceptionTelemetry(exception);
            exTelemetry.Properties["earthquakeId"] = earthquakeId;
            exTelemetry.Properties["context"]      = EventWebhookFailed;
            _telemetryClient.TrackException(exTelemetry);
        }

        var properties = new Dictionary<string, string>
        {
            ["earthquakeId"] = earthquakeId
        };

        if (httpStatusCode.HasValue)
            properties["httpStatusCode"] = httpStatusCode.Value.ToString();

        _telemetryClient.TrackEvent(EventWebhookFailed, properties);
    }
}
