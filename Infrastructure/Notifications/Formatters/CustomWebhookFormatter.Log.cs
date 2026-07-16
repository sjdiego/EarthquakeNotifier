using System;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Notifications.Formatters
{
    public partial class CustomWebhookFormatter
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse custom webhook template as JSON after placeholder replacement")]
        private static partial void LogInvalidJsonTemplate(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error in custom webhook formatter")]
        private static partial void LogFormatterError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Template placeholders replaced successfully")]
        private static partial void LogPlaceholdersReplaced(ILogger logger);
    }
}
