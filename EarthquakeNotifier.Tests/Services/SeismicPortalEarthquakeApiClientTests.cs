using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EarthquakeNotifier.Infrastructure.Api.SeismicPortal;
using EarthquakeNotifier.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace EarthquakeNotifier.Tests.Services
{
    /// <summary>
    /// Unit tests for SeismicPortalEarthquakeApiClient.
    /// Tests API response parsing, magnitude filtering, and error handling.
    /// </summary>
    public class SeismicPortalEarthquakeApiClientTests
    {
        private readonly Mock<ILogger<SeismicPortalEarthquakeApiClient>> _loggerMock;

        public SeismicPortalEarthquakeApiClientTests()
        {
            _loggerMock = new Mock<ILogger<SeismicPortalEarthquakeApiClient>>();
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithMultipleEvents_ParsesSuccessfully()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SeismicPortalApiResponseFixture.GetSampleMultipleEarthquakes())
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.True(result.IsSuccess);
            var earthquakes = result.Value!;
            Assert.NotNull(earthquakes);
            Assert.Equal(2, earthquakes.Count); // Only 2 events have magnitude >= 4.0
            Assert.All(earthquakes, e => Assert.True(e.Magnitude >= 4.0));
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_FiltersEventsByMagnitude_ExcludesBelowMinimum()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SeismicPortalApiResponseFixture.GetSampleMultipleEarthquakes())
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(5.0);

            // Assert
            Assert.True(result.IsSuccess);
            var earthquakes = result.Value!;
            Assert.Single(earthquakes);
            Assert.Equal(5.3, earthquakes.First().Magnitude);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithEmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SeismicPortalApiResponseFixture.GetSampleEmptyResponse())
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithHttpError_ReturnsFailureResult()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.IsType<HttpRequestException>(result.Exception);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithTimeoutException_ReturnsFailureResult()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timeout"));

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.IsType<TaskCanceledException>(result.Exception);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithMalformedJson_ReturnsFailureResult()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SeismicPortalApiResponseFixture.GetSampleMalformedResponse())
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.IsType<System.Text.Json.JsonException>(result.Exception);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_PropertiesMappedCorrectly_VerifiesAllFields()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SeismicPortalApiResponseFixture.GetSampleSingleEarthquake())
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.True(result.IsSuccess);
            var earthquakes = result.Value!;
            Assert.Single(earthquakes);
            var earthquake = earthquakes.First();
            Assert.Equal("ep2024_003457", earthquake.EarthquakeId);
            Assert.Equal(6.1, earthquake.Magnitude);
            Assert.Equal("50 km E of Istanbul, Turkey", earthquake.Place);
            Assert.Equal(30.5, earthquake.Longitude);
            Assert.Equal(41.0, earthquake.Latitude);
            Assert.Equal(15.0, earthquake.Depth);
            Assert.Contains("seismicportal.eu", earthquake.Url);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithMissingMagnitude_HandlesGracefully()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SeismicPortalApiResponseFixture.GetSampleResponseWithMissingMagnitude())
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithInvalidGeometry_HandlesGracefully()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SeismicPortalApiResponseFixture.GetSampleResponseWithInvalidGeometry())
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object);
            var apiClient = new SeismicPortalEarthquakeApiClient(client, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert — event is returned with 0,0 coords when geometry is missing but properties.lat/lon are absent too
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal(0.0, result.Value![0].Latitude);
            Assert.Equal(0.0, result.Value![0].Longitude);
        }
    }
}
