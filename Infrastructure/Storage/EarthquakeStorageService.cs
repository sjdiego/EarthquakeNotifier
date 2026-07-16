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
    /// <remarks>
    /// Initializes the service with the blob container that stores processed earthquake events.
    /// </remarks>
    public partial class EarthquakeStorageService(BlobContainerClient containerClient, ILogger<EarthquakeStorageService> logger) : IEarthquakeStorageService
    {
        private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        /// <inheritdoc/>
        /// <remarks>
        /// Uses an <c>IfNoneMatch: *</c> condition so the upload is atomic — concurrent function
        /// instances racing on the same event will get a 409/412 and return <c>false</c> safely.
        /// </remarks>
        public async Task<bool> TrySaveAsync(EarthquakeNotification earthquake)
        {
            var blobName = $"{earthquake.EarthquakeId}.json";
            var jsonContent = JsonSerializer.Serialize(earthquake, _options);
            var binaryData = BinaryData.FromString(jsonContent);
            var blobClient = containerClient.GetBlobClient(blobName);

            LogUploading(logger, earthquake.EarthquakeId, blobName);

            try
            {
                await blobClient.UploadAsync(binaryData, new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
                });

                LogSaved(logger, earthquake.EarthquakeId);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
            {
                // 409 Conflict or 412 Precondition Failed — blob already exists
                return false;
            }
        }

        [LoggerMessage(Level = LogLevel.Debug, Message = "Uploading earthquake {earthquakeId} to blob storage as {blobName}")]
        private static partial void LogUploading(ILogger logger, string earthquakeId, string blobName);

        [LoggerMessage(Level = LogLevel.Information, Message = "Saved earthquake {earthquakeId} to blob storage")]
        private static partial void LogSaved(ILogger logger, string earthquakeId);
    }
}
