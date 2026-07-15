using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EarthquakeNotifier.Domain;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Storage
{
    /// <summary>
    /// Persists earthquake events to Azure Blob Storage using an atomic IfNoneMatch condition
    /// to prevent duplicate processing across concurrent function instances.
    /// </summary>
    public class EarthquakeStorageService : IEarthquakeStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<EarthquakeStorageService> _logger;

        /// <summary>
        /// Initializes the service with the blob container that stores processed earthquake events.
        /// </summary>
        public EarthquakeStorageService(BlobContainerClient containerClient, ILogger<EarthquakeStorageService> logger)
        {
            _containerClient = containerClient;
            _logger          = logger;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Uses an <c>IfNoneMatch: *</c> condition so the upload is atomic — concurrent function
        /// instances racing on the same event will get a 409/412 and return <c>false</c> safely.
        /// </remarks>
        public async Task<bool> TrySaveAsync(EarthquakeNotification earthquake)
        {
            var blobName    = $"{earthquake.EarthquakeId}.json";
            var jsonContent = JsonSerializer.Serialize(earthquake, new JsonSerializerOptions { WriteIndented = true });
            var binaryData  = BinaryData.FromString(jsonContent);
            var blobClient  = _containerClient.GetBlobClient(blobName);

            _logger.LogDebug("Uploading earthquake {earthquakeId} to blob storage as {blobName}", earthquake.EarthquakeId, blobName);

            try
            {
                await blobClient.UploadAsync(binaryData, new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
                });

                _logger.LogInformation("Saved earthquake {earthquakeId} to blob storage", earthquake.EarthquakeId);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
            {
                // 409 Conflict or 412 Precondition Failed — blob already exists
                return false;
            }
        }
    }
}
