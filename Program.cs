using System;
using System.Net.Http;
using Azure.Storage.Blobs;
using EarthquakeNotifier.Infrastructure.Api;
using EarthquakeNotifier.Infrastructure.Api.SeismicPortal;
using EarthquakeNotifier.Infrastructure.Api.Usgs;
using EarthquakeNotifier.Infrastructure.Notifications;
using EarthquakeNotifier.Infrastructure.Notifications.Formatters;
using EarthquakeNotifier.Infrastructure.Storage;
using EarthquakeNotifier.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var configuration = hostContext.Configuration;

        // Register HTTP clients via IHttpClientFactory
        services.AddHttpClient();  // default client for API calls
        services.AddHttpClient("webhook", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register Azure Blob Storage — uses the full connection string directly
        var storageConnection = configuration["AzureWebJobsStorage"] ?? string.Empty;
        services.AddSingleton(new BlobServiceClient(storageConnection));
        services.AddSingleton(provider =>
        {
            var container = provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("earthquakes");
            container.CreateIfNotExists();
            return container;
        });

        // Register the earthquake API client based on the configured provider
        var apiProvider = configuration["EARTHQUAKE_API_PROVIDER"]?.ToLowerInvariant() ?? "usgs";
        if (apiProvider == "seismicportal")
            services.AddScoped<IEarthquakeApiClient, SeismicPortalEarthquakeApiClient>();
        else
            services.AddScoped<IEarthquakeApiClient, UsgsEarthquakeApiClient>();

        // Register webhook formatters
        services.AddScoped<CustomWebhookFormatter>();
        services.AddScoped<WebhookFormatterFactory>();

        // Register storage and webhook notification services
        services.AddScoped<IEarthquakeStorageService, EarthquakeStorageService>();
        services.AddScoped<IWebhookNotificationService>(provider => new WebhookNotificationService(
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<WebhookFormatterFactory>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WebhookNotificationService>>(),
            provider.GetRequiredService<EarthquakeMetrics>(),
            new WebhookConfig(
                BaseUrl: configuration["WEBHOOK_BASE_URL"],
                Dest: configuration["WEBHOOK_DEST"],
                Token: configuration["WEBHOOK_TOKEN"])));

        // Register Application Insights metrics tracker
        services.AddSingleton<EarthquakeMetrics>();
    })
    .Build();

host.Run();
