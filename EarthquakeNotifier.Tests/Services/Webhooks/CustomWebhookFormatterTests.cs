using System;
using System.Net.Http;
using System.Threading.Tasks;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Notifications;
using EarthquakeNotifier.Infrastructure.Notifications.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EarthquakeNotifier.Tests.Services.Webhooks
{
    /// <summary>
    /// Unit tests for CustomWebhookFormatter.
    /// Tests placeholder replacement, edge cases, and JSON validity.
    /// </summary>
    public class CustomWebhookFormatterTests
    {
        private static readonly WebhookConfig DefaultConfig = new(BaseUrl: "https://example.com/webhook", Dest: null, Token: null);

        private readonly EarthquakeNotification _sampleEarthquake;
        private readonly Mock<ILogger<CustomWebhookFormatter>> _loggerMock;

        public CustomWebhookFormatterTests()
        {
            _sampleEarthquake = new EarthquakeNotification
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

            _loggerMock = new Mock<ILogger<CustomWebhookFormatter>>();
        }

        private static async Task<string> ReadBodyAsync(HttpRequestMessage request)
            => await request.Content!.ReadAsStringAsync();

        [Fact]
        public async Task CustomWebhookFormatter_ReplacesAllPlaceholders_Successfully()
        {
            var configMock = new Mock<IConfiguration>();
            var template = @"{ ""event"": ""{earthquakeId}"", ""magnitude"": {magnitude}, ""location"": ""{place}"" }";
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns(template);

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, _sampleEarthquake);

            Assert.Equal(HttpMethod.Post, request.Method);
            var json = await ReadBodyAsync(request);
            Assert.Contains("us7000kp60", json);
            Assert.Contains("6.50", json);
            Assert.Contains("San Francisco", json);
        }

        [Fact]
        public async Task CustomWebhookFormatter_HandlesSpecialCharacters_InPlaceholders()
        {
            var earthquakeWithSpecialChars = new EarthquakeNotification
            {
                EarthquakeId = "test\"id",
                Magnitude = 5.0,
                Place = "S\u00e3o Paulo, Brazil - \"Test\" Location",
                Time = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                Latitude = -23.55,
                Longitude = -46.63,
                Depth = 20,
                Url = "https://example.com?q=test&value=\"quoted\""
            };

            var configMock = new Mock<IConfiguration>();
            var template = @"{ ""place"": ""{place}"", ""url"": ""{url}"" }";
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns(template);

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, earthquakeWithSpecialChars);

            var json = await ReadBodyAsync(request);
            Assert.NotNull(json);
            Assert.True(json.Length > 0);
        }

        [Fact]
        public async Task CustomWebhookFormatter_WithMissingPlaceholders_OutputsTemplateAsIs()
        {
            var configMock = new Mock<IConfiguration>();
            var template = @"{ ""static_field"": ""no_placeholders"", ""value"": 123 }";
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns(template);

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, _sampleEarthquake);

            var json = await ReadBodyAsync(request);
            Assert.Contains("static_field", json);
            Assert.Contains("no_placeholders", json);
        }

        [Fact]
        public async Task CustomWebhookFormatter_WithInvalidJson_ReturnsErrorObject()
        {
            var configMock = new Mock<IConfiguration>();
            var invalidTemplate = @"{ ""invalid"": invalid json }";
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns(invalidTemplate);

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, _sampleEarthquake);

            var json = await ReadBodyAsync(request);
            Assert.Contains("error", json);
        }

        [Fact]
        public async Task CustomWebhookFormatter_WithEmptyTemplate_ReturnsEmptyObject()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns("{}");

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, _sampleEarthquake);

            var json = await ReadBodyAsync(request);
            Assert.Equal("{}", json);
        }

        [Fact]
        public async Task CustomWebhookFormatter_WithNestedJsonTemplate_PreservesStructure()
        {
            var configMock = new Mock<IConfiguration>();
            var nestedTemplate = @"{ 
                ""event"": {
                    ""id"": ""{earthquakeId}"",
                    ""magnitude"": {magnitude},
                    ""location"": { ""place"": ""{place}"", ""lat"": {latitude}, ""lon"": {longitude} }
                },
                ""metadata"": { ""timestamp"": ""{timestamp}"" }
            }";
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns(nestedTemplate);

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, _sampleEarthquake);

            var json = await ReadBodyAsync(request);
            Assert.Contains("us7000kp60", json);
            Assert.Contains("37.8", json);
            Assert.Contains("San Francisco", json);
        }

        [Fact]
        public async Task CustomWebhookFormatter_AllPlaceholders_AreReplaced()
        {
            var configMock = new Mock<IConfiguration>();
            var template = @"{
                ""earthquakeId"": ""{earthquakeId}"",
                ""magnitude"": {magnitude},
                ""place"": ""{place}"",
                ""time"": ""{time}"",
                ""latitude"": {latitude},
                ""longitude"": {longitude},
                ""depth"": {depth},
                ""url"": ""{url}"",
                ""timestamp"": ""{timestamp}""
            }";
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns(template);

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, _sampleEarthquake);

            var json = await ReadBodyAsync(request);
            Assert.Contains("us7000kp60", json);
            Assert.Contains("6.50", json);
            Assert.Contains("39 km ENE of San Francisco, California", json);
            Assert.DoesNotContain("{", json.Substring(1));
        }

        [Fact]
        public async Task CustomWebhookFormatter_WithMissingConfiguration_UsesEmptyTemplate()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns((string?)null);

            var formatter = new CustomWebhookFormatter(configMock.Object, _loggerMock.Object);
            var request = formatter.BuildRequest(DefaultConfig, _sampleEarthquake);

            var json = await ReadBodyAsync(request);
            Assert.Equal("{}", json);
        }
    }
}
