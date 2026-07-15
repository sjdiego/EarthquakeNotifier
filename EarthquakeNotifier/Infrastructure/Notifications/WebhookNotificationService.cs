using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Telemetry;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Notifications
{
    /// <summary>
    /// Sends formatted webhook notifications for earthquake events via HTTP.
    /// </summary>
    public class WebhookNotificationService : IWebhookNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WebhookFormatterFactory _formatterFactory;
        private readonly ILogger<WebhookNotificationService> _logger;
        private readonly WebhookConfig _config;
        private readonly string _safeBaseUrl;
        private readonly EarthquakeMetrics _metrics;

        public WebhookNotificationService(
            IHttpClientFactory httpClientFactory,
            WebhookFormatterFactory formatterFactory,
            ILogger<WebhookNotificationService> logger,
            EarthquakeMetrics metrics,
            WebhookConfig config)
        {
            _httpClientFactory = httpClientFactory;
            _formatterFactory  = formatterFactory;
            _logger            = logger;
            _metrics           = metrics;
            _config            = config;
            _safeBaseUrl       = MaskUrl(config.BaseUrl ?? config.Dest);
        }

        public async Task SendAsync(EarthquakeNotification earthquake)
        {
            if (string.IsNullOrWhiteSpace(_config.Token) &&
                string.IsNullOrWhiteSpace(_config.Dest)  &&
                string.IsNullOrWhiteSpace(_config.BaseUrl))
            {
                _logger.LogWarning("Webhook not configured (WEBHOOK_TOKEN/WEBHOOK_DEST/WEBHOOK_BASE_URL missing), skipping notification for {earthquakeId}", earthquake.EarthquakeId);
                return;
            }

            try
            {
                var formatter = _formatterFactory.GetFormatter();
                var request   = formatter.BuildRequest(_config, earthquake);

                _logger.LogDebug("Sending webhook to {safeUrl} for {earthquakeId}", _safeBaseUrl, earthquake.EarthquakeId);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    var bodyForLog = await request.Content!.ReadAsStringAsync();
                    _logger.LogTrace("Webhook request body for {earthquakeId}: {body}", earthquake.EarthquakeId, bodyForLog);
                    request.Content = new StringContent(bodyForLog, Encoding.UTF8, "application/json");
                }

                var response = await _httpClientFactory.CreateClient("webhook").SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        var successBody = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug("Webhook response for {earthquakeId}: {responseBody}", earthquake.EarthquakeId, successBody);
                    }
                    else
                    {
                        _logger.LogInformation("Webhook notification sent successfully for {earthquakeId}", earthquake.EarthquakeId);
                    }
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Webhook failed [{statusCode}] at {safeUrl} for {earthquakeId}. Response: {responseBody}",
                        response.StatusCode, _safeBaseUrl, earthquake.EarthquakeId, responseBody);
                    _metrics.TrackWebhookFailed(earthquake.EarthquakeId, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending webhook notification for {earthquakeId}", earthquake.EarthquakeId);
                _metrics.TrackWebhookFailed(earthquake.EarthquakeId, exception: ex);
            }
        }

        /// <summary>Masks a URL for safe logging: shows only scheme://host/***</summary>
        private static string MaskUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "(not set)";
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "***";
            return $"{uri.Scheme}://{uri.Host}/***";
        }
    }
}
