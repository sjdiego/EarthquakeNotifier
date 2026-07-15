using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Notifications;

namespace EarthquakeNotifier.Infrastructure.Notifications.Formatters
{
    /// <summary>
    /// Formatter for Telegram Bot API using the sendRichMessage endpoint (Bot API 10+).
    /// <list type="bullet">
    ///   <item><description><b>BaseUrl</b>: Telegram server root. Defaults to https://api.telegram.org (override for custom/local Bot API servers).</description></item>
    ///   <item><description><b>Token</b>: Bot credential in the form {bot_id}:{token} (Key Vault secret).</description></item>
    ///   <item><description><b>Dest</b>: Telegram chat_id (e.g. -1001212121212).</description></item>
    /// </list>
    /// </summary>
    public class TelegramWebhookFormatter : IWebhookNotificationFormatter
    {
        private const string DefaultBaseUrl = "https://api.telegram.org";

        public HttpRequestMessage BuildRequest(WebhookConfig config, EarthquakeNotification notification)
        {
            if (string.IsNullOrWhiteSpace(config.Dest))
                throw new ArgumentException("WEBHOOK_DEST (chat_id) is required for Telegram notifications.", nameof(config));

            var baseUrl = (config.BaseUrl?.TrimEnd('/') is { Length: > 0 } b) ? b : DefaultBaseUrl;
            var richUrl = $"{baseUrl}/bot{config.Token}/sendRichMessage";
            var chatId  = config.Dest;

            var mag   = notification.Magnitude.ToString("F1", CultureInfo.InvariantCulture);
            var depth = notification.Depth.ToString("F1", CultureInfo.InvariantCulture);
            var lat   = notification.Latitude.ToString("F4", CultureInfo.InvariantCulture);
            var lon   = notification.Longitude.ToString("F4", CultureInfo.InvariantCulture);

            var emoji  = notification.Magnitude >= 6.0 ? "\uD83D\uDEA8" : notification.Magnitude >= 4.0 ? "\uD83D\uDD34" : "\uD83C\uDF0E";
            var header = notification.Magnitude >= 6.0 ? "Major Earthquake Alert"
                       : notification.Magnitude >= 4.0 ? "Earthquake Alert"
                       : "Seismic Activity";

            // Rich HTML text (Bot API 10+ sendRichMessage format)
            // tg-map zoom range is 13-20; 13 = most zoomed out (regional context)
            var richText =
                $"<h1>{emoji} {header}</h1>" +
                $"<hr/>" +
                $"<table>" +
                $"<tr><td><b>Location</b></td><td>{notification.Place}</td></tr>" +
                $"<tr><td><b>Magnitude</b></td><td><b>M{mag}</b></td></tr>" +
                $"<tr><td><b>Depth</b></td><td>{depth} km</td></tr>" +
                $"<tr><td><b>Time</b></td><td>{notification.Time:yyyy-MM-dd HH:mm:ss} UTC</td></tr>" +
                $"</table>" +
                $"<figure><tg-map lat=\"{lat}\" long=\"{lon}\" zoom=\"13\"></tg-map>" +
                $"<figcaption>{lat}, {lon}</figcaption></figure>";

            var payload = new
            {
                chat_id      = chatId,
                rich_message = new
                {
                    html                  = richText,
                    skip_entity_detection = true
                },
                reply_markup = new
                {
                    inline_keyboard = new[]
                    {
                        new[] { new { text = "\uD83D\uDD17 More information", url = notification.Url } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            return new HttpRequestMessage(HttpMethod.Post, richUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}