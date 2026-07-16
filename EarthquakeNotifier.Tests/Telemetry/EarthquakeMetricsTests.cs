using System;
using System.Net.Http;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;

namespace EarthquakeNotifier.Tests.Telemetry
{
    public class EarthquakeMetricsTests
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly FakeTelemetryChannel _channel;
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

        public EarthquakeMetricsTests()
        {
            _channel = new FakeTelemetryChannel();
            var config = new TelemetryConfiguration { TelemetryChannel = _channel, ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000" };
            _telemetryClient = new TelemetryClient(config);
            _metrics = new EarthquakeMetrics(_telemetryClient);
        }

        [Fact]
        public void TrackEarthquakeProcessed_SendsEventWithCorrectName()
        {
            _metrics.TrackEarthquakeProcessed(Sample);

            var evt = Assert.Single(_channel.Items, i => i is EventTelemetry e && e.Name == "EarthquakeProcessed");
            var eventTelemetry = Assert.IsType<EventTelemetry>(evt);
            Assert.Equal("EarthquakeProcessed", eventTelemetry.Name);
        }

        [Fact]
        public void TrackEarthquakeProcessed_IncludesAllProperties()
        {
            _metrics.TrackEarthquakeProcessed(Sample);

            var evt = Assert.IsType<EventTelemetry>(Assert.Single(_channel.Items));
            Assert.Equal(Sample.EarthquakeId, evt.Properties["earthquakeId"]);
            Assert.Equal(Sample.Place, evt.Properties["place"]);
            Assert.Equal(Sample.Magnitude.ToString("F1"), evt.Properties["magnitude"]);
            Assert.Equal(Sample.Depth.ToString("F1"), evt.Properties["depth"]);
            Assert.Equal(Sample.Url, evt.Properties["url"]);
        }

        [Fact]
        public void TrackApiFailed_WithoutException_SendsEventOnly()
        {
            _metrics.TrackApiFailed("USGS", "timeout");

            var evt = Assert.IsType<EventTelemetry>(Assert.Single(_channel.Items));
            Assert.Equal("EarthquakeApiFailed", evt.Name);
            Assert.Equal("USGS", evt.Properties["provider"]);
            Assert.Equal("timeout", evt.Properties["errorMessage"]);
        }

        [Fact]
        public void TrackApiFailed_WithException_SendsExceptionAndEvent()
        {
            var ex = new HttpRequestException("connection refused");

            _metrics.TrackApiFailed("SeismicPortal", "connection refused", ex);

            Assert.Equal(2, _channel.Items.Count);
            Assert.Contains(_channel.Items, i => i is ExceptionTelemetry e && e.Exception == ex);
            Assert.Contains(_channel.Items, i => i is EventTelemetry e && e.Name == "EarthquakeApiFailed");
        }

        [Fact]
        public void TrackApiFailed_Exception_IncludesProviderProperty()
        {
            var ex = new InvalidOperationException("fail");
            _metrics.TrackApiFailed("USGS", "fail", ex);

            var exTelemetry = Assert.IsType<ExceptionTelemetry>(
                Assert.Single(_channel.Items, i => i is ExceptionTelemetry));
            Assert.Equal("USGS", exTelemetry.Properties["provider"]);
        }

        [Fact]
        public void TrackWebhookFailed_WithStatusCode_IncludesStatusCodeProperty()
        {
            _metrics.TrackWebhookFailed("us7000kp60", httpStatusCode: 429);

            var evt = Assert.IsType<EventTelemetry>(Assert.Single(_channel.Items));
            Assert.Equal("WebhookFailed", evt.Name);
            Assert.Equal("us7000kp60", evt.Properties["earthquakeId"]);
            Assert.Equal("429", evt.Properties["httpStatusCode"]);
        }

        [Fact]
        public void TrackWebhookFailed_WithoutStatusCode_OmitsStatusCodeProperty()
        {
            _metrics.TrackWebhookFailed("us7000kp60");

            var evt = Assert.IsType<EventTelemetry>(Assert.Single(_channel.Items));
            Assert.DoesNotContain("httpStatusCode", evt.Properties.Keys);
        }

        [Fact]
        public void TrackWebhookFailed_WithException_SendsExceptionAndEvent()
        {
            var ex = new HttpRequestException("webhook unreachable");
            _metrics.TrackWebhookFailed("us7000kp60", exception: ex);

            Assert.Equal(2, _channel.Items.Count);
            Assert.Contains(_channel.Items, i => i is ExceptionTelemetry e && e.Exception == ex);
            Assert.Contains(_channel.Items, i => i is EventTelemetry e && e.Name == "WebhookFailed");
        }

        /// <summary>In-memory ITelemetryChannel for testing without network calls.</summary>
        private sealed class FakeTelemetryChannel : ITelemetryChannel
        {
            public System.Collections.Generic.List<ITelemetry> Items { get; } = new();
            public bool? DeveloperMode { get; set; }
            public string? EndpointAddress { get; set; }
            public void Send(ITelemetry item) => Items.Add(item);
            public void Flush() { }
            public void Dispose() { }
        }
    }
}
