using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EarthquakeNotifier.Domain;
using EarthquakeNotifier.Infrastructure.Notifications;
using EarthquakeNotifier.Infrastructure.Notifications.Formatters;
using Xunit;

namespace EarthquakeNotifier.Tests.Services.Webhooks
{
    /// <summary>
    /// Unit tests for webhook formatters.
    /// Verifies that BuildRequest produces the correct HttpRequestMessage for each service.
    /// </summary>
    public class WebhookFormatterTests
    {
        private readonly EarthquakeNotification _sampleEarthquake;

        public WebhookFormatterTests()
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
        }

        private static async Task<JsonElement> ReadBodyAsync(HttpRequestMessage request)
        {
            var json = await request.Content!.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        [Fact]
        public async Task NtfyWebhookFormatter_BuildRequest_ProducesCorrectStructure()
        {
            var formatter = new NtfyWebhookFormatter();
            var config = new WebhookConfig(BaseUrl: "https://ntfy.sh", Dest: "test-topic", Token: null);
            var request = formatter.BuildRequest(config, _sampleEarthquake);

            // Must POST to the instance root
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://ntfy.sh/", request.RequestUri!.ToString());
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);

            // Body is JSON with all ntfy fields including topic
            var body = await ReadBodyAsync(request);
            Assert.True(body.TryGetProperty("topic", out var topic));
            Assert.True(body.TryGetProperty("title", out var title));
            Assert.True(body.TryGetProperty("message", out _));
            Assert.True(body.TryGetProperty("priority", out _));
            Assert.True(body.TryGetProperty("click", out _));
            Assert.Equal("test-topic", topic.GetString());
            Assert.Contains("6.5", title.GetString() ?? string.Empty);
        }

        [Fact]
        public async Task NtfyWebhookFormatter_BuildRequest_SendsTokenAsAuthorizationHeader()
        {
            var formatter = new NtfyWebhookFormatter();
            var config = new WebhookConfig(BaseUrl: "https://ntfy.example.com", Dest: "my-topic", Token: "tk_abc123");
            var request = formatter.BuildRequest(config, _sampleEarthquake);

            // Token must be sent as Authorization header, not in query string
            Assert.True(request.Headers.TryGetValues("Authorization", out var values));
            Assert.Contains("Bearer tk_abc123", values);
            Assert.Null(request.RequestUri!.Query.Length > 0 ? request.RequestUri.Query : null);

            // Topic is taken from Dest
            var body = await ReadBodyAsync(request);
            Assert.Equal("my-topic", body.GetProperty("topic").GetString());
        }

        [Fact]
        public async Task TelegramWebhookFormatter_BuildRequest_ProducesCorrectStructure()
        {
            var formatter = new TelegramWebhookFormatter();
            var config = new WebhookConfig(BaseUrl: "https://api.telegram.org", Dest: "-1001234567890", Token: "123:TOKEN");
            var request = formatter.BuildRequest(config, _sampleEarthquake);

            Assert.Equal(HttpMethod.Post, request.Method);
            // Must use sendRichMessage endpoint
            Assert.Contains("sendRichMessage", request.RequestUri!.AbsolutePath);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);

            var body = await ReadBodyAsync(request);
            // chat_id is in the body
            Assert.True(body.TryGetProperty("chat_id", out var chatId));
            Assert.True(body.TryGetProperty("rich_message", out var richMessage));
            Assert.True(body.TryGetProperty("reply_markup", out var replyMarkup));
            Assert.Equal("-1001234567890", chatId.GetString());

            Assert.True(richMessage.TryGetProperty("html", out var text));
            Assert.False(richMessage.TryGetProperty("parse_mode", out _));
            Assert.Contains("6.5", text.GetString() ?? string.Empty);
            Assert.Contains("San Francisco", text.GetString() ?? string.Empty);
            Assert.True(replyMarkup.TryGetProperty("inline_keyboard", out _));
        }

        [Fact]
        public async Task TelegramWebhookFormatter_BuildRequest_UsesDefaultBaseUrl()
        {
            var formatter = new TelegramWebhookFormatter();
            // No BaseUrl → should default to https://api.telegram.org
            var config = new WebhookConfig(BaseUrl: null, Dest: "-1001", Token: "123:TOKEN");
            var request = formatter.BuildRequest(config, _sampleEarthquake);

            Assert.Contains("api.telegram.org", request.RequestUri!.Host);
        }

        [Fact]
        public void TelegramWebhookFormatter_BuildRequest_ThrowsWhenDestMissing()
        {
            var formatter = new TelegramWebhookFormatter();
            var config = new WebhookConfig(BaseUrl: null, Dest: null, Token: "123:TOKEN");

            Assert.Throws<ArgumentException>(() => formatter.BuildRequest(config, _sampleEarthquake));
        }

        [Fact]
        public void TelegramWebhookFormatter_BuildRequest_ThrowsWhenDestEmpty()
        {
            var formatter = new TelegramWebhookFormatter();
            var config = new WebhookConfig(BaseUrl: null, Dest: "  ", Token: "123:TOKEN");

            Assert.Throws<ArgumentException>(() => formatter.BuildRequest(config, _sampleEarthquake));
        }

        [Fact]
        public async Task DiscordWebhookFormatter_BuildRequest_ProducesCorrectStructure()
        {
            var formatter = new DiscordWebhookFormatter();
            var config = new WebhookConfig(BaseUrl: null, Dest: null, Token: "123456/abcdeftoken");
            var request = formatter.BuildRequest(config, _sampleEarthquake);

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
            // URL must contain the token path
            Assert.Contains("123456/abcdeftoken", request.RequestUri!.ToString());

            var body = await ReadBodyAsync(request);
            Assert.True(body.TryGetProperty("embeds", out var embeds));
            Assert.True(embeds.GetArrayLength() > 0);

            var embed = embeds[0];
            Assert.True(embed.TryGetProperty("title", out var title));
            Assert.True(embed.TryGetProperty("fields", out var fields));
            Assert.Contains("6.5", title.GetString() ?? string.Empty);
            Assert.True(fields.GetArrayLength() > 0);
        }

        [Fact]
        public async Task GenericWebhookFormatter_BuildRequest_ProducesCorrectStructure()
        {
            var formatter = new GenericWebhookFormatter();
            var config = new WebhookConfig(BaseUrl: "https://example.com/webhook", Dest: null, Token: null);
            var request = formatter.BuildRequest(config, _sampleEarthquake);

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);

            var body = await ReadBodyAsync(request);
            Assert.True(body.TryGetProperty("earthquakeId", out var id));
            Assert.True(body.TryGetProperty("magnitude", out var magnitude));
            Assert.True(body.TryGetProperty("place", out _));
            Assert.True(body.TryGetProperty("latitude", out _));
            Assert.True(body.TryGetProperty("longitude", out _));
            Assert.True(body.TryGetProperty("depth", out _));
            Assert.True(body.TryGetProperty("url", out _));

            Assert.Equal("us7000kp60", id.GetString());
            Assert.Equal(6.5, magnitude.GetDouble());
        }

        [Fact]
        public async Task DiscordWebhookFormatter_MagnitudeColors_VariesByIntensity()
        {
            var formatter = new DiscordWebhookFormatter();
            var minorEarthquake = new EarthquakeNotification
            {
                EarthquakeId = "test1",
                Magnitude = 4.5,
                Place = "Test Location",
                Time = DateTime.UtcNow,
                Latitude = 0,
                Longitude = 0,
                Depth = 10,
                Url = "http://test"
            };

            var req1 = formatter.BuildRequest(new WebhookConfig(null, null, "123/token"), _sampleEarthquake);
            var req2 = formatter.BuildRequest(new WebhookConfig(null, null, "456/token"), minorEarthquake);

            var body1 = await ReadBodyAsync(req1);
            var body2 = await ReadBodyAsync(req2);

            var color1 = body1.GetProperty("embeds")[0].GetProperty("color").GetInt32();
            var color2 = body2.GetProperty("embeds")[0].GetProperty("color").GetInt32();

            Assert.NotEqual(color1, color2);
            Assert.True(color1 > 0 && color2 > 0);
        }

        [Fact]
        public async Task AllFormatters_HandleSpecialCharacters_InLocation()
        {
            var earthquakeWithSpecialChars = new EarthquakeNotification
            {
                EarthquakeId = "test_special",
                Magnitude = 5.0,
                Place = "100 km NE of S\u00e3o Paulo, Brazil (Latitude: -23.55\u00b0)",
                Time = DateTime.UtcNow,
                Latitude = -23.55,
                Longitude = -46.63,
                Depth = 20,
                Url = "http://test?param=value&other=data"
            };

            (IWebhookNotificationFormatter formatter, WebhookConfig config)[] cases =
            [
                (new NtfyWebhookFormatter(),     new WebhookConfig("https://ntfy.sh",          "topic", null)),
                (new TelegramWebhookFormatter(), new WebhookConfig("https://api.telegram.org", "-100",  "123:TOKEN")),
                (new DiscordWebhookFormatter(),  new WebhookConfig(null,                       null,    "123/token")),
                (new GenericWebhookFormatter(),  new WebhookConfig("https://example.com/hook", null,    null))
            ];

            foreach (var (fmt, cfg) in cases)
            {
                var request = fmt.BuildRequest(cfg, earthquakeWithSpecialChars);
                var body = await request.Content!.ReadAsStringAsync();
                Assert.NotNull(body);
                Assert.True(body.Length > 0);
            }
        }
    }
}
