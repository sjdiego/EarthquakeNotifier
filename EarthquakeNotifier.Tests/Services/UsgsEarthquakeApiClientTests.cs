using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using EarthquakeNotifier.Infrastructure.Api.Usgs;
using EarthquakeNotifier.Tests.Fixtures;

namespace EarthquakeNotifier.Tests.Services
{
    /// <summary>
    /// Unit tests for UsgsEarthquakeApiClient.
    /// Tests API response parsing, magnitude filtering, and error handling.
    /// </summary>
    public class UsgsEarthquakeApiClientTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<UsgsEarthquakeApiClient>> _loggerMock;

        public UsgsEarthquakeApiClientTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<UsgsEarthquakeApiClient>>();
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithMultipleEvents_ParsesSuccessfully()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(UsgsApiResponseFixture.GetSampleMultipleEarthquakes())
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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

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
            var httpClientMock = new Mock<HttpClient>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(UsgsApiResponseFixture.GetSampleMultipleEarthquakes())
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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(5.0);

            // Assert
            Assert.True(result.IsSuccess);
            var earthquakes = result.Value!;
            Assert.Single(earthquakes);
            Assert.Equal(5.2, earthquakes.First().Magnitude);
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithEmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(UsgsApiResponseFixture.GetSampleEmptyResponse())
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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

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
                Content = new StringContent(UsgsApiResponseFixture.GetSampleMalformedResponse())
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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

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
                Content = new StringContent(UsgsApiResponseFixture.GetSampleSingleEarthquake())
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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.True(result.IsSuccess);
            var earthquakes = result.Value!;
            Assert.Single(earthquakes);
            var earthquake = earthquakes.First();
            Assert.Equal("us7000kp60", earthquake.EarthquakeId);
            Assert.Equal(6.5, earthquake.Magnitude);
            Assert.Equal("39 km ENE of San Francisco, California", earthquake.Place);
            Assert.Equal(-121.8, earthquake.Longitude);
            Assert.Equal(37.8, earthquake.Latitude);
            Assert.Equal(12.5, earthquake.Depth);
            Assert.True(earthquake.Url.Contains("earthquake.usgs.gov"));
        }

        [Fact]
        public async Task GetRecentEarthquakesAsync_WithMissingMagnitude_HandlesGracefully()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(UsgsApiResponseFixture.GetSampleResponseWithMissingMagnitude())
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
            var apiClient = new UsgsEarthquakeApiClient(client, _configurationMock.Object, _loggerMock.Object);

            // Act
            var result = await apiClient.GetRecentEarthquakesAsync(4.0);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!); // Events without magnitude should be filtered out
        }
    }
}
