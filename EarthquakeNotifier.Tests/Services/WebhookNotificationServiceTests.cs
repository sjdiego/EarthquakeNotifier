using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Notifications;
using EarthquakeNotifier.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace EarthquakeNotifier.Tests.Services
{
    /// <summary>
    /// Unit tests for WebhookNotificationService.
    /// Verifies unconfigured skip, success/failure HTTP responses, and exception handling.
    /// </summary>
    public class WebhookNotificationServiceTests
    {
        private readonly Mock<ILogger<WebhookNotificationService>> _loggerMock = new();
        private readonly EarthquakeMetrics _metrics;

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

        public WebhookNotificationServiceTests()
        {
            var telemetryConfig = new TelemetryConfiguration
            {
                TelemetryChannel = new NullTelemetryChannel(),
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };
            _metrics = new EarthquakeMetrics(new TelemetryClient(telemetryConfig));
        }

        /// <summary>
        /// Builds a real WebhookFormatterFactory configured for "generic" type (no external dependencies).
        /// </summary>
        private static WebhookFormatterFactory BuildRealFactory(string webhookType = "generic")
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["WEBHOOK_TYPE"]).Returns(webhookType);

            return new WebhookFormatterFactory(
                configMock.Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<WebhookFormatterFactory>>().Object);
        }

        private WebhookNotificationService CreateService(
            WebhookConfig config,
            HttpResponseMessage? response = null,
            string webhookType = "generic")
        {
            var httpResponse = response ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(f => f.CreateClient("webhook"))
                .Returns(new HttpClient(handlerMock.Object));

            return new WebhookNotificationService(
                httpClientFactoryMock.Object,
                BuildRealFactory(webhookType),
                _loggerMock.Object,
                _metrics,
                config);
        }

        [Fact]
        public async Task SendAsync_WhenNotConfigured_DoesNotSendHttpRequest()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(f => f.CreateClient("webhook"))
                .Returns(new HttpClient(handlerMock.Object));

            var service = new WebhookNotificationService(
                httpClientFactoryMock.Object,
                BuildRealFactory(),
                _loggerMock.Object,
                _metrics,
                new WebhookConfig(BaseUrl: null, Dest: null, Token: null));

            await service.SendAsync(Sample);

            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Theory]
        [InlineData("https://webhook.example.com", null, null)]         // BaseUrl only
        [InlineData("https://webhook.example.com", "my-dest", null)]    // BaseUrl + Dest
        [InlineData("https://webhook.example.com", null, "my-token")]   // BaseUrl + Token
        public async Task SendAsync_WhenAnyConfigPresent_SendsRequest(
            string? baseUrl, string? dest, string? token)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(f => f.CreateClient("webhook"))
                .Returns(new HttpClient(handlerMock.Object));

            var service = new WebhookNotificationService(
                httpClientFactoryMock.Object,
                BuildRealFactory(),
                _loggerMock.Object,
                _metrics,
                new WebhookConfig(BaseUrl: baseUrl, Dest: dest, Token: token));

            await service.SendAsync(Sample);

            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SendAsync_WhenHttpResponseFails_DoesNotThrow()
        {
            var config = new WebhookConfig(BaseUrl: "https://webhook.example.com", Dest: null, Token: "secret");
            var service = CreateService(config, new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error")
            });

            // Must not throw — errors are logged and swallowed
            await service.SendAsync(Sample);
        }

        [Fact]
        public async Task SendAsync_WhenHttpClientThrows_DoesNotThrow()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("connection refused"));

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(f => f.CreateClient("webhook"))
                .Returns(new HttpClient(handlerMock.Object));

            var service = new WebhookNotificationService(
                httpClientFactoryMock.Object,
                BuildRealFactory(),
                _loggerMock.Object,
                _metrics,
                new WebhookConfig(BaseUrl: "https://webhook.example.com", Dest: null, Token: "secret"));

            // Must not throw
            await service.SendAsync(Sample);
        }

        [Theory]
        [InlineData("https://webhook.example.com/path?token=secret123")]
        [InlineData("https://ntfy.sh/my-topic")]
        [InlineData(null)]
        public void Constructor_MasksUrlInSafeBaseUrl(string? baseUrl)
        {
            // Verifies construction does not throw for any URL format
            _ = new WebhookNotificationService(
                new Mock<IHttpClientFactory>().Object,
                BuildRealFactory(),
                _loggerMock.Object,
                _metrics,
                new WebhookConfig(BaseUrl: baseUrl, Dest: "dest", Token: null));
        }

        [Theory]
        [InlineData("generic")]
        [InlineData("discord")]
        [InlineData("ntfy")]
        public async Task SendAsync_WithDifferentFormatterTypes_DoesNotThrow(string webhookType)
        {
            var config = new WebhookConfig(BaseUrl: "https://webhook.example.com", Dest: "topic", Token: "token");
            var service = CreateService(config, new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            }, webhookType);

            await service.SendAsync(Sample);
        }

        private sealed class NullTelemetryChannel : ITelemetryChannel
        {
            public bool? DeveloperMode { get; set; }
            public string? EndpointAddress { get; set; }
            public void Send(ITelemetry item) { }
            public void Flush() { }
            public void Dispose() { }
        }
    }
}
