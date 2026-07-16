using System.Net.Http;
using EarthquakeNotifier.Domain;

namespace EarthquakeNotifier.Infrastructure.Notifications
{
    /// <summary>
    /// Interface for building service-specific HTTP requests for earthquake webhook notifications.
    /// Each implementation controls its own URL construction, headers, content-type and body format.
    /// </summary>
    public interface IWebhookNotificationFormatter
    {
        /// <summary>
        /// Builds a ready-to-send HTTP request from the webhook configuration and earthquake notification.
        /// </summary>
        /// <param name="config">Webhook configuration: base URL, destination and secret token.</param>
        /// <param name="notification">The earthquake notification to include in the request.</param>
        public HttpRequestMessage BuildRequest(WebhookConfig config, EarthquakeNotification notification);
    }
}
