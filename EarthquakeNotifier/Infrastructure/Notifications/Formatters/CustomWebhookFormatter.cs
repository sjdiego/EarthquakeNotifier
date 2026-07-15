using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Notifications;

namespace EarthquakeNotifier.Infrastructure.Notifications.Formatters
{
    /// <summary>
    /// Custom formatter that allows placeholder replacement in JSON templates.
    /// Reads WEBHOOK_TEMPLATE_CUSTOM from configuration and replaces placeholders.
    /// Supported placeholders: {earthquakeId}, {magnitude}, {place}, {time}, {latitude}, {longitude}, {depth}, {url}
    /// </summary>
    public class CustomWebhookFormatter : IWebhookNotificationFormatter
    {
        private readonly string _customTemplate;
        private readonly ILogger<CustomWebhookFormatter> _logger;

        public CustomWebhookFormatter(
            IConfiguration configuration,
            ILogger<CustomWebhookFormatter> logger)
        {
            _customTemplate = configuration["WEBHOOK_TEMPLATE_CUSTOM"] ?? "{}";
            _logger = logger;
        }

        /// <summary>
        /// Builds an HTTP request using the custom template with placeholder replacement.
        /// <list type="bullet">
        ///   <item><description><b>BaseUrl</b>: full destination URL (WEBHOOK_BASE_URL).</description></item>
        ///   <item><description><b>Dest</b>: not used.</description></item>
        ///   <item><description><b>Token</b>: optional; sent as Authorization header if provided.</description></item>
        /// </list>
        /// </summary>
        public HttpRequestMessage BuildRequest(WebhookConfig config, EarthquakeNotification notification)
        {
            string json;
            try
            {
                var replacedTemplate = ReplaceTemplatePlaceholders(_customTemplate, notification);
                // Validate that the template produces valid JSON
                JsonSerializer.Deserialize<JsonElement>(replacedTemplate);
                json = replacedTemplate;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse custom webhook template as JSON after placeholder replacement");
                json = JsonSerializer.Serialize(new { error = "Invalid JSON template", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in custom webhook formatter");
                json = JsonSerializer.Serialize(new { error = "Formatter error", details = ex.Message });
            }

            var request = new HttpRequestMessage(HttpMethod.Post, config.BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(config.Token))
                request.Headers.TryAddWithoutValidation("Authorization", config.Token);

            return request;
        }

        /// <summary>
        /// Replaces all earthquake-related placeholders in the template string.
        /// </summary>
        private string ReplaceTemplatePlaceholders(string template, EarthquakeNotification notification)
        {
            var result = template
                .Replace("{earthquakeId}", EscapeJsonValue(notification.EarthquakeId))
                .Replace("{magnitude}", notification.Magnitude.ToString("F2", CultureInfo.InvariantCulture))
                .Replace("{place}", EscapeJsonValue(notification.Place))
                .Replace("{time}", notification.Time.ToString("o", CultureInfo.InvariantCulture))
                .Replace("{latitude}", notification.Latitude.ToString("F6", CultureInfo.InvariantCulture))
                .Replace("{longitude}", notification.Longitude.ToString("F6", CultureInfo.InvariantCulture))
                .Replace("{depth}", notification.Depth.ToString("F2", CultureInfo.InvariantCulture))
                .Replace("{url}", EscapeJsonValue(notification.Url))
                .Replace("{timestamp}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            _logger.LogDebug("Template placeholders replaced successfully");
            return result;
        }

        /// <summary>
        /// Escapes string values for safe JSON inclusion.
        /// Only escapes backslash and double-quote to keep inline template JSON valid.
        /// </summary>
        private static string EscapeJsonValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }
    }
}
