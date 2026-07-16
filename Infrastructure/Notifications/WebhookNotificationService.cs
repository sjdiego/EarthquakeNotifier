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
    public partial class WebhookNotificationService(
        IHttpClientFactory httpClientFactory,
        WebhookFormatterFactory formatterFactory,
        ILogger<WebhookNotificationService> logger,
        EarthquakeMetrics metrics,
        WebhookConfig config) : IWebhookNotificationService
    {
        private readonly string _safeBaseUrl = MaskUrl(config.BaseUrl ?? config.Dest);

        public async Task SendAsync(EarthquakeNotification earthquake)
        {
            if (string.IsNullOrWhiteSpace(config.Token) &&
                string.IsNullOrWhiteSpace(config.Dest) &&
                string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                LogWebhookNotConfigured(logger, earthquake.EarthquakeId);
                return;
            }

            try
            {
                IWebhookNotificationFormatter formatter = formatterFactory.GetFormatter();
                HttpRequestMessage request = formatter.BuildRequest(config, earthquake);

                LogSendingWebhook(logger, _safeBaseUrl, earthquake.EarthquakeId);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    string bodyForLog = await request.Content!.ReadAsStringAsync();
                    LogWebhookRequestBody(logger, earthquake.EarthquakeId, bodyForLog);
                    request.Content = new StringContent(bodyForLog, Encoding.UTF8, "application/json");
                }

                HttpResponseMessage response = await httpClientFactory.CreateClient("webhook").SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        string successBody = await response.Content.ReadAsStringAsync();
                        LogWebhookResponse(logger, earthquake.EarthquakeId, successBody);
                    }
                    else
                    {
                        LogWebhookSent(logger, earthquake.EarthquakeId);
                    }
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    LogWebhookFailed(logger, response.StatusCode, _safeBaseUrl, earthquake.EarthquakeId, responseBody);
                    metrics.TrackWebhookFailed(earthquake.EarthquakeId, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                LogWebhookError(logger, ex, earthquake.EarthquakeId);
                metrics.TrackWebhookFailed(earthquake.EarthquakeId, exception: ex);
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
