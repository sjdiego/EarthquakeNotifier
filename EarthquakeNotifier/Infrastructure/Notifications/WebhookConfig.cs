namespace EarthquakeNotifier.Infrastructure.Notifications
{
    /// <summary>
    /// Holds the three configuration values that together identify a webhook destination.
    /// <list type="bullet">
    ///   <item><description><b>BaseUrl</b> — server root, e.g. https://api.telegram.org. Non-secret; read from WEBHOOK_BASE_URL app setting.</description></item>
    ///   <item><description><b>Dest</b> — service-specific destination: chat_id for Telegram, topic for ntfy, empty for Discord. Non-secret; read from WEBHOOK_DEST app setting.</description></item>
    ///   <item><description><b>Token</b> — secret credential: bot_id:token for Telegram, Bearer token for ntfy, id/token for Discord. Read from WEBHOOK_TOKEN (Key Vault secret).</description></item>
    /// </list>
    /// </summary>
    public record WebhookConfig(string? BaseUrl, string? Dest, string? Token);
}
