using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace EarthquakeNotifier.Infrastructure.Notifications
{
    public partial class WebhookNotificationService
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook not configured (WEBHOOK_TOKEN/WEBHOOK_DEST/WEBHOOK_BASE_URL missing), skipping notification for {earthquakeId}")]
        private static partial void LogWebhookNotConfigured(ILogger logger, string earthquakeId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Sending webhook to {safeUrl} for {earthquakeId}")]
        private static partial void LogSendingWebhook(ILogger logger, string safeUrl, string earthquakeId);

        [LoggerMessage(Level = LogLevel.Trace, Message = "Webhook request body for {earthquakeId}: {body}")]
        private static partial void LogWebhookRequestBody(ILogger logger, string earthquakeId, string body);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Webhook response for {earthquakeId}: {responseBody}")]
        private static partial void LogWebhookResponse(ILogger logger, string earthquakeId, string responseBody);

        [LoggerMessage(Level = LogLevel.Information, Message = "Webhook notification sent successfully for {earthquakeId}")]
        private static partial void LogWebhookSent(ILogger logger, string earthquakeId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook failed [{statusCode}] at {safeUrl} for {earthquakeId}. Response: {responseBody}")]
        private static partial void LogWebhookFailed(ILogger logger, HttpStatusCode statusCode, string safeUrl, string earthquakeId, string responseBody);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error sending webhook notification for {earthquakeId}")]
        private static partial void LogWebhookError(ILogger logger, Exception ex, string earthquakeId);
    }
}
