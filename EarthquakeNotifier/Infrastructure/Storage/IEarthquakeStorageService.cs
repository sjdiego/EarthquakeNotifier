using System.Threading.Tasks;
using EarthquakeNotifier.Domain;

namespace EarthquakeNotifier.Infrastructure.Storage
{
    /// <summary>
    /// Handles persistence of earthquake events to prevent duplicate notifications.
    /// </summary>
    public interface IEarthquakeStorageService
    {
        /// <summary>
        /// Atomically saves the earthquake event.
        /// Returns true if saved (new event), false if it already existed (duplicate).
        /// </summary>
        Task<bool> TrySaveAsync(EarthquakeNotification earthquake);
    }
}
