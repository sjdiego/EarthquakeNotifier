using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EarthquakeNotifier.Domain;

namespace EarthquakeNotifier.Infrastructure.Notifications.Formatters
{
    /// <summary>
    /// Formatter for ntfy notification service (self-hosted or ntfy.sh).
    /// Uses the JSON publish API: POST to {BaseUrl} with topic in the body.
    /// <list type="bullet">
    ///   <item><description><b>BaseUrl</b>: ntfy server root, e.g. https://ntfy.sh (WEBHOOK_BASE_URL).</description></item>
    ///   <item><description><b>Dest</b>: ntfy topic, e.g. earthquakes (WEBHOOK_DEST).</description></item>
    ///   <item><description><b>Token</b>: access token sent as Authorization: Bearer header (Key Vault secret).</description></item>
    /// </list>
    /// </summary>
    public class NtfyWebhookFormatter : IWebhookNotificationFormatter
    {
        public HttpRequestMessage BuildRequest(WebhookConfig config, EarthquakeNotification notification)
        {
            var baseUrl = config.BaseUrl?.TrimEnd('/') ?? string.Empty;
            var topic = config.Dest?.Trim('/') is { Length: > 0 } t ? t : "earthquakes";

            var mag = notification.Magnitude.ToString("F1", CultureInfo.InvariantCulture);
            var depth = notification.Depth.ToString("F1", CultureInfo.InvariantCulture);
            var priority = notification.Magnitude >= 6.0 ? 5 : notification.Magnitude >= 4.0 ? 4 : 3;
            var emoji = notification.Magnitude >= 6.0 ? "\uD83D\uDEA8" : notification.Magnitude >= 4.0 ? "\uD83D\uDD34" : "\uD83C\uDF0E";
            var tags = notification.Magnitude >= 6.0 ? new[] { "rotating_light", "earth_americas" }
                         : notification.Magnitude >= 4.0 ? new[] { "red_circle", "earth_americas" }
                         : new[] { "earth_americas" };

            var payload = new
            {
                topic,
                title = $"{emoji} Earthquake M{mag} \u2014 {notification.Place}",
                message = $"Depth: {depth} km | Time: {notification.Time:yyyy-MM-dd HH:mm} UTC",
                tags,
                priority,
                click = notification.Url
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(config.Token))
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {config.Token}");

            return request;
        }
    }
}

