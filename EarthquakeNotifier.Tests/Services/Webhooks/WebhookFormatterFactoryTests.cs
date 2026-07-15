using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EarthquakeNotifier.Infrastructure.Notifications;
using EarthquakeNotifier.Infrastructure.Notifications.Formatters;

namespace EarthquakeNotifier.Tests.Services.Webhooks
{
    /// <summary>
    /// Unit tests for WebhookFormatterFactory.
    /// Tests factory selection, fallback behavior, and case sensitivity.
    /// </summary>
    public class WebhookFormatterFactoryTests
    {
        private readonly Mock<ILogger<WebhookFormatterFactory>> _loggerMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;

        public WebhookFormatterFactoryTests()
        {
            _loggerMock = new Mock<ILogger<WebhookFormatterFactory>>();
            _serviceProviderMock = new Mock<IServiceProvider>();
        }

        [Theory]
        [InlineData("ntfy")]
        [InlineData("NTFY")]
        [InlineData("Ntfy")]
        public void GetFormatter_WithNtfyType_ReturnsNtfyFormatter(string webhookType)
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter(webhookType);

            // Assert
            Assert.IsType<NtfyWebhookFormatter>(formatter);
        }

        [Theory]
        [InlineData("telegram")]
        [InlineData("TELEGRAM")]
        [InlineData("Telegram")]
        public void GetFormatter_WithTelegramType_ReturnsTelegramFormatter(string webhookType)
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter(webhookType);

            // Assert
            Assert.IsType<TelegramWebhookFormatter>(formatter);
        }

        [Theory]
        [InlineData("discord")]
        [InlineData("DISCORD")]
        [InlineData("Discord")]
        public void GetFormatter_WithDiscordType_ReturnsDiscordFormatter(string webhookType)
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter(webhookType);

            // Assert
            Assert.IsType<DiscordWebhookFormatter>(formatter);
        }

        [Theory]
        [InlineData("generic")]
        [InlineData("GENERIC")]
        [InlineData("Generic")]
        public void GetFormatter_WithGenericType_ReturnsGenericFormatter(string webhookType)
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter(webhookType);

            // Assert
            Assert.IsType<GenericWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetFormatter_WithUnknownType_FallsBackToGenericFormatter()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter("unknown_type");

            // Assert
            Assert.IsType<GenericWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetFormatter_WithEmptyType_FallsBackToGenericFormatter()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter("");

            // Assert
            Assert.IsType<GenericWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetFormatter_WithNullType_FallsBackToGenericFormatter()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter(null!);

            // Assert
            Assert.IsType<GenericWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetFormatter_FromConfiguration_UsesWebhookTypeEnvironmentVariable()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["WEBHOOK_TYPE"]).Returns("discord");

            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter();

            // Assert
            Assert.IsType<DiscordWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetFormatter_WithMissingConfiguration_DefaultsToGeneric()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["WEBHOOK_TYPE"]).Returns((string?)null);

            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter();

            // Assert
            Assert.IsType<GenericWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetFormatter_WithCustomType_AttemptsToResolveFromDI()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["WEBHOOK_TEMPLATE_CUSTOM"]).Returns("{\"test\":\"value\"}");
            var loggerMock = new Mock<ILogger<CustomWebhookFormatter>>();
            var customFormatter = new CustomWebhookFormatter(configMock.Object, loggerMock.Object);

            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(CustomWebhookFormatter)))
                .Returns(customFormatter);

            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter("custom");

            // Assert
            Assert.NotNull(formatter);
            Assert.IsType<CustomWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetFormatter_WithCustomTypeDIFailure_FallsBackToGeneric()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(CustomWebhookFormatter)))
                .Throws<InvalidOperationException>();

            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            // Act
            var formatter = factory.GetFormatter("custom");

            // Assert
            Assert.IsType<GenericWebhookFormatter>(formatter);
        }

        [Fact]
        public void GetSupportedTypes_ReturnsAllSupportedFormatters()
        {
            // Act
            var supported = WebhookFormatterFactory.GetSupportedTypes();

            // Assert
            Assert.NotNull(supported);
            Assert.Contains("ntfy", supported);
            Assert.Contains("telegram", supported);
            Assert.Contains("discord", supported);
            Assert.Contains("generic", supported);
            Assert.Contains("custom", supported);
            Assert.Equal(5, supported.Length);
        }

        [Fact]
        public void GetFormatter_CaseSensitivity_HandlesAllCases()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            var factory = new WebhookFormatterFactory(configMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

            var types = new[] { "NTFY", "Telegram", "dIsCord", "GENERIC" };
            var expectedTypes = new[]
            {
                typeof(NtfyWebhookFormatter),
                typeof(TelegramWebhookFormatter),
                typeof(DiscordWebhookFormatter),
                typeof(GenericWebhookFormatter)
            };

            // Act & Assert
            for (int i = 0; i < types.Length; i++)
            {
                var formatter = factory.GetFormatter(types[i]);
                Assert.IsType(expectedTypes[i], formatter);
            }
        }
    }
}
