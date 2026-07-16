using System;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Notifications
{
    public partial class WebhookFormatterFactory
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Creating webhook formatter of type: {webhookType}")]
        private static partial void LogCreatingFormatter(ILogger logger, string webhookType);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Empty webhook type provided, defaulting to generic")]
        private static partial void LogEmptyWebhookType(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to resolve CustomWebhookFormatter from DI container, falling back to generic")]
        private static partial void LogCustomFormatterResolveFailed(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown webhook type '{webhookType}' requested, defaulting to generic formatter")]
        private static partial void LogUnknownWebhookType(ILogger logger, string webhookType);
    }
}
