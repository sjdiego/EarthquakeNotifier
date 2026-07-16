using System;
using EarthquakeNotifier.Infrastructure.Notifications.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Notifications
{
    /// <summary>
    /// Factory for creating webhook notification formatters based on configuration.
    /// Supports: ntfy, telegram, discord, generic, custom
    /// </summary>
    public partial class WebhookFormatterFactory(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<WebhookFormatterFactory> logger)
    {

        /// <summary>
        /// Creates a webhook formatter instance based on WEBHOOK_TYPE configuration.
        /// </summary>
        /// <returns>IWebhookNotificationFormatter implementation</returns>
        public IWebhookNotificationFormatter GetFormatter()
        {
            var webhookType = configuration["WEBHOOK_TYPE"]?.ToLowerInvariant() ?? "generic";

            LogCreatingFormatter(logger, webhookType);

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
                LogEmptyWebhookType(logger);
                return new GenericWebhookFormatter();
            }

            LogCreatingFormatter(logger, webhookType);

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
                return serviceProvider.GetRequiredService<CustomWebhookFormatter>();
            }
            catch (Exception ex)
            {
                LogCustomFormatterResolveFailed(logger, ex);
                return new GenericWebhookFormatter();
            }
        }

        /// <summary>
        /// Handles unknown webhook types by logging and defaulting to generic.
        /// </summary>
        private GenericWebhookFormatter HandleUnknownType(string webhookType)
        {
            LogUnknownWebhookType(logger, webhookType);
            return new GenericWebhookFormatter();
        }

        /// <summary>
        /// Gets the list of supported formatter types.
        /// </summary>
        public static string[] GetSupportedTypes()
        {
            return ["ntfy", "telegram", "discord", "generic", "custom"];
        }
    }
}
