using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Notifications;

namespace EarthquakeNotifier.Infrastructure.Notifications.Formatters
{
    /// <summary>
    /// Generic JSON formatter for standard webhook notifications.
    /// Produces a structured JSON payload with all earthquake details.
    /// <list type="bullet">
    ///   <item><description><b>BaseUrl</b>: full destination URL (WEBHOOK_BASE_URL).</description></item>
    ///   <item><description><b>Dest</b>: not used.</description></item>
    ///   <item><description><b>Token</b>: optional; sent as Authorization header if provided.</description></item>
    /// </list>
    /// </summary>
    public class GenericWebhookFormatter : IWebhookNotificationFormatter
    {
        public HttpRequestMessage BuildRequest(WebhookConfig config, EarthquakeNotification notification)
        {
            var payload = new
            {
                earthquakeId = notification.EarthquakeId,
                magnitude    = notification.Magnitude,
                place        = notification.Place,
                time         = notification.Time.ToString("o"),
                latitude     = notification.Latitude,
                longitude    = notification.Longitude,
                depth        = notification.Depth,
                url          = notification.Url,
                timestamp    = DateTime.UtcNow.ToString("o")
            };

            var json    = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, config.BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(config.Token))
                request.Headers.TryAddWithoutValidation("Authorization", config.Token);

            return request;
        }
    }
}
