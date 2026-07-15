using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EarthquakeNotifier.Infrastructure.Notifications.Formatters;

namespace EarthquakeNotifier.Infrastructure.Notifications
{
    /// <summary>
    /// Factory for creating webhook notification formatters based on configuration.
    /// Supports: ntfy, telegram, discord, generic, custom
    /// </summary>
    public class WebhookFormatterFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WebhookFormatterFactory> _logger;

        public WebhookFormatterFactory(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<WebhookFormatterFactory> logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Creates a webhook formatter instance based on WEBHOOK_TYPE configuration.
        /// </summary>
        /// <returns>IWebhookNotificationFormatter implementation</returns>
        public IWebhookNotificationFormatter GetFormatter()
        {
            var webhookType = _configuration["WEBHOOK_TYPE"]?.ToLowerInvariant() ?? "generic";

            _logger.LogInformation("Creating webhook formatter of type: {WebhookType}", webhookType);

            return webhookType switch
            {
                "ntfy" => new NtfyWebhookFormatter(),
                "telegram" => new TelegramWebhookFormatter(),
                "discord" => new DiscordWebhookFormatter(),
                "custom" => GetCustomFormatter(),
                "generic" => new GenericWebhookFormatter(),
                _ => HandleUnknownType(webhookType)
            };
        }

        /// <summary>
        /// Creates a formatter with a specified type override.
        /// </summary>
        public IWebhookNotificationFormatter GetFormatter(string webhookType)
        {
            if (string.IsNullOrWhiteSpace(webhookType))
            {
                _logger.LogWarning("Empty webhook type provided, defaulting to generic");
                return new GenericWebhookFormatter();
            }

            _logger.LogInformation("Creating webhook formatter of type: {WebhookType}", webhookType);

            return webhookType.ToLowerInvariant() switch
            {
                "ntfy" => new NtfyWebhookFormatter(),
                "telegram" => new TelegramWebhookFormatter(),
                "discord" => new DiscordWebhookFormatter(),
                "custom" => GetCustomFormatter(),
                "generic" => new GenericWebhookFormatter(),
                _ => HandleUnknownType(webhookType)
            };
        }

        /// <summary>
        /// Gets the custom formatter with proper dependency injection.
        /// </summary>
        private IWebhookNotificationFormatter GetCustomFormatter()
        {
            try
            {
                return _serviceProvider.GetRequiredService<CustomWebhookFormatter>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve CustomWebhookFormatter from DI container, falling back to generic");
                return new GenericWebhookFormatter();
            }
        }

        /// <summary>
        /// Handles unknown webhook types by logging and defaulting to generic.
        /// </summary>
        private IWebhookNotificationFormatter HandleUnknownType(string webhookType)
        {
            _logger.LogWarning("Unknown webhook type '{WebhookType}' requested, defaulting to generic formatter", webhookType);
            return new GenericWebhookFormatter();
        }

        /// <summary>
        /// Gets the list of supported formatter types.
        /// </summary>
        public static string[] GetSupportedTypes()
        {
            return new[] { "ntfy", "telegram", "discord", "generic", "custom" };
        }
    }
}
