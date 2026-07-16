using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EarthquakeNotifier.Tests.Services
{
    /// <summary>
    /// Unit tests for EarthquakeStorageService.
    /// Verifies blob upload, deduplication via IfNoneMatch, and blob naming.
    /// </summary>
    public class EarthquakeStorageServiceTests
    {
        private readonly Mock<ILogger<EarthquakeStorageService>> _loggerMock = new();

        private static readonly EarthquakeNotification Sample = new()
        {
            EarthquakeId = "us7000kp60",
            Magnitude = 6.5,
            Place = "39 km ENE of San Francisco, California",
            Time = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Latitude = 37.8,
            Longitude = -121.8,
            Depth = 12.5,
            Url = "https://earthquake.usgs.gov/earthquakes/eventpage/us7000kp60/executive"
        };

        private (EarthquakeStorageService service, Mock<BlobClient> blobClientMock) CreateService(
            Action<Mock<BlobClient>>? configureBlobClient = null)
        {
            var blobClientMock = new Mock<BlobClient>();
            configureBlobClient?.Invoke(blobClientMock);

            var containerMock = new Mock<BlobContainerClient>();
            containerMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            return (new EarthquakeStorageService(containerMock.Object, _loggerMock.Object), blobClientMock);
        }

        [Fact]
        public async Task TrySaveAsync_NewEarthquake_UploadsAndReturnsTrue()
        {
            var (service, blobClientMock) = CreateService(mock =>
                mock.Setup(b => b.UploadAsync(
                        It.IsAny<BinaryData>(),
                        It.IsAny<BlobUploadOptions>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Mock<Response<BlobContentInfo>>().Object));

            var result = await service.TrySaveAsync(Sample);

            Assert.True(result);
            blobClientMock.Verify(
                b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(409)]
        [InlineData(412)]
        public async Task TrySaveAsync_BlobAlreadyExists_ReturnsFalse(int statusCode)
        {
            var (service, _) = CreateService(mock =>
                mock.Setup(b => b.UploadAsync(
                        It.IsAny<BinaryData>(),
                        It.IsAny<BlobUploadOptions>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RequestFailedException(statusCode, "Conflict")));

            var result = await service.TrySaveAsync(Sample);

            Assert.False(result);
        }

        [Fact]
        public async Task TrySaveAsync_UnexpectedStorageError_PropagatesException()
        {
            var (service, _) = CreateService(mock =>
                mock.Setup(b => b.UploadAsync(
                        It.IsAny<BinaryData>(),
                        It.IsAny<BlobUploadOptions>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RequestFailedException(500, "Internal Server Error")));

            await Assert.ThrowsAsync<RequestFailedException>(() => service.TrySaveAsync(Sample));
        }

        [Fact]
        public async Task TrySaveAsync_BlobNameIsEarthquakeIdDotJson()
        {
            var containerMock = new Mock<BlobContainerClient>();
            var blobClientMock = new Mock<BlobClient>();

            blobClientMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<BinaryData>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<BlobContentInfo>>().Object);

            containerMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            var service = new EarthquakeStorageService(containerMock.Object, _loggerMock.Object);
            await service.TrySaveAsync(Sample);

            containerMock.Verify(c => c.GetBlobClient($"{Sample.EarthquakeId}.json"), Times.Once);
        }

        [Fact]
        public async Task TrySaveAsync_UploadOptionsUseIfNoneMatchCondition()
        {
            BlobUploadOptions? capturedOptions = null;

            var blobClientMock = new Mock<BlobClient>();
            blobClientMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<BinaryData>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<BinaryData, BlobUploadOptions, CancellationToken>((_, opts, _) => capturedOptions = opts)
                .ReturnsAsync(new Mock<Response<BlobContentInfo>>().Object);

            var containerMock = new Mock<BlobContainerClient>();
            containerMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);

            var service = new EarthquakeStorageService(containerMock.Object, _loggerMock.Object);
            await service.TrySaveAsync(Sample);

            Assert.NotNull(capturedOptions?.Conditions);
            Assert.Equal(ETag.All, capturedOptions!.Conditions!.IfNoneMatch);
        }

        [Fact]
        public async Task TrySaveAsync_SerializesEarthquakeDataToJson()
        {
            BinaryData? capturedData = null;

            var blobClientMock = new Mock<BlobClient>();
            blobClientMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<BinaryData>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<BinaryData, BlobUploadOptions, CancellationToken>((data, _, _) => capturedData = data)
                .ReturnsAsync(new Mock<Response<BlobContentInfo>>().Object);

            var containerMock = new Mock<BlobContainerClient>();
            containerMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);

            var service = new EarthquakeStorageService(containerMock.Object, _loggerMock.Object);
            await service.TrySaveAsync(Sample);

            var json = capturedData!.ToString();
            Assert.Contains(Sample.EarthquakeId, json);
            Assert.Contains("6.5", json);
            Assert.Contains("San Francisco", json);
        }
    }
}
