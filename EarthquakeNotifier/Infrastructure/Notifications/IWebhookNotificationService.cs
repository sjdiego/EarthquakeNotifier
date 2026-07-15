using System.Threading.Tasks;
using EarthquakeNotifier.Domain;

namespace EarthquakeNotifier.Infrastructure.Notifications
{
    /// <summary>
    /// Sends formatted webhook notifications for earthquake events.
    /// </summary>
    public interface IWebhookNotificationService
    {
        /// <summary>
        /// Formats and dispatches a webhook notification for the given earthquake event.
        /// </summary>
        /// <param name="earthquake">The earthquake event to notify about.</param>
        Task SendAsync(EarthquakeNotification earthquake);
    }
}
