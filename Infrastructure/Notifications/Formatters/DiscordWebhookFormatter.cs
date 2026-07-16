using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EarthquakeNotifier.Domain;

namespace EarthquakeNotifier.Infrastructure.Notifications.Formatters
{
    /// <summary>
    /// Formatter for Discord webhook API. Produces rich embeds with structured earthquake information.
    /// <list type="bullet">
    ///   <item><description><b>BaseUrl</b>: Discord API root. Defaults to https://discord.com/api/webhooks.</description></item>
    ///   <item><description><b>Token</b>: Discord webhook credential in the form {webhook_id}/{webhook_token} (Key Vault secret).</description></item>
    ///   <item><description><b>Dest</b>: not used for Discord.</description></item>
    /// </list>
    /// </summary>
    public class DiscordWebhookFormatter : IWebhookNotificationFormatter
    {
        private const string DefaultBaseUrl = "https://discord.com/api/webhooks";

        public HttpRequestMessage BuildRequest(WebhookConfig config, EarthquakeNotification notification)
        {
            string baseUrl = (config.BaseUrl?.TrimEnd('/') is { Length: > 0 } b) ? b : DefaultBaseUrl;
            string webhookUrl = $"{baseUrl}/{config.Token}";

            var payload = new
            {
                content = "\uD83D\uDD34 **Earthquake Alert**",
                embeds = new object[]
                {
                    new
                    {
                        title       = $"Earthquake - Magnitude {notification.Magnitude.ToString("F1", CultureInfo.InvariantCulture)}",
                        description = notification.Place,
                        color       = GetColorByMagnitude(notification.Magnitude),
                        fields = new object[]
                        {
                            new {
                                name = "Magnitude",
                                value = notification.Magnitude.ToString("F1", CultureInfo.InvariantCulture),
                                inline = true
                            },
                            new {
                                name = "Depth",
                                value = $"{notification.Depth.ToString("F1", CultureInfo.InvariantCulture)} km",
                                inline = true
                            },
                            new {
                                name = "Time (UTC)",
                                value = notification.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                                inline = false
                            },
                            new {
                                name = "Coordinates",
                                value = $"Lat: {notification.Latitude.ToString("F4", CultureInfo.InvariantCulture)}\nLon: {notification.Longitude.ToString("F4", CultureInfo.InvariantCulture)}",
                                inline = false
                            },
                            new {
                                name = "Location",
                                value = notification.Place,
                                inline = false
                            }
                        },
                        url       = notification.Url,
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            string json = JsonSerializer.Serialize(payload);
            return new HttpRequestMessage(HttpMethod.Post, webhookUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private static int GetColorByMagnitude(double magnitude) => magnitude switch
        {
            >= 8.0 => 0xFF0000,
            >= 7.0 => 0xFF6600,
            >= 6.0 => 0xFFCC00,
            >= 5.0 => 0x99CC00,
            _ => 0x0099CC
        };
    }
}
