# 🌍 EarthquakeNotifier

> A serverless Azure Function that monitors global seismic activity and sends real-time notifications to your preferred platform.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Azure Functions v4](https://img.shields.io/badge/Azure%20Functions-v4-0078D4?logo=azure-functions)](https://learn.microsoft.com/en-us/azure/azure-functions/)
[![xUnit](https://img.shields.io/badge/Tests-xUnit-green)](https://xunit.net/)

---

## ✨ Features

- ⏱️ **Timer-triggered** — configurable cron schedule (e.g. every minute)
- 🌐 **Multiple data sources** — [USGS Earthquake Hazards Program](https://earthquake.usgs.gov/) and [SeismicPortal (EMSC)](https://www.seismicportal.eu/)
- 🔔 **Multiple notification channels** — Telegram, ntfy, Discord, generic webhook, or fully custom JSON template
- 🔁 **Deduplication** — Azure Blob Storage prevents duplicate notifications using atomic `IfNoneMatch` uploads
- 🔐 **Secrets management** — sensitive credentials stored in Azure Key Vault, resolved via Managed Identity
- 📊 **Application Insights** — custom metrics for processed earthquakes, API failures, and webhook errors
- 🏗️ **Clean Architecture** — Domain / Infrastructure / Telemetry layer separation

---

## 🏛️ Architecture

```
EarthquakeNotifier/
├── Domain/
│   └── EarthquakeNotification.cs        # Core domain model
├── Infrastructure/
│   ├── Api/
│   │   ├── IEarthquakeApiClient.cs
│   │   ├── Usgs/                        # USGS GeoJSON client + models
│   │   └── SeismicPortal/               # SeismicPortal FDSN client + models
│   ├── Notifications/
│   │   ├── WebhookNotificationService.cs
│   │   ├── WebhookFormatterFactory.cs
│   │   └── Formatters/                  # Telegram, ntfy, Discord, Generic, Custom
│   └── Storage/
│       └── EarthquakeStorageService.cs  # Blob-based deduplication
├── Telemetry/
│   └── EarthquakeMetrics.cs             # Application Insights custom metrics
├── EarthquakeMonitorFunction.cs         # Azure Function entry point
└── Program.cs                           # DI registration
```

---

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (local storage emulator)

### Run locally

```bash
git clone https://github.com/sjdiego/EarthquakeNotifier.git
cd EarthquakeNotifier
```

Create a `local.settings.json` (never commit this file):

```json
{
  "IsEncrypted": false,
  "Values": {
	"AzureWebJobsStorage": "UseDevelopmentStorage=true",
	"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
	"EARTHQUAKE_API_PROVIDER": "usgs",
	"MIN_EARTHQUAKE_MAGNITUDE": "4.0",
	"EARTHQUAKE_SCHEDULE": "0 * * * * *",
	"WEBHOOK_TYPE": "telegram",
	"WEBHOOK_BASE_URL": "https://api.telegram.org",
	"WEBHOOK_DEST": "<your_chat_id>",
	"WEBHOOK_TOKEN": "<your_bot_id:token>",
	"APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=00000000-0000-0000-0000-000000000000"
  }
}
```

```bash
func start
```

---

## 🔔 Notification Channels

Configure `WEBHOOK_TYPE` to switch between providers:

| `WEBHOOK_TYPE` | `WEBHOOK_BASE_URL` | `WEBHOOK_DEST` | `WEBHOOK_TOKEN` |
|---|---|---|---|
| `telegram` | `https://api.telegram.org` (or custom) | Chat ID (required) | `{bot_id}:{token}` |
| `ntfy` | ntfy server root, e.g. `https://ntfy.sh` | Topic name | Bearer token |
| `discord` | `https://discord.com/api/webhooks` (or custom) | _(empty)_ | `{webhook_id}/{token}` |
| `generic` | Full destination URL | _(empty)_ | Optional `Authorization` header |
| `custom` | Full destination URL | _(empty)_ | Optional `Authorization` header |

### 📱 Telegram

Sends a rich HTML message via Bot API 10+ `sendRichMessage` endpoint with an inline map, formatted table, and a link button.

> Requires `WEBHOOK_DEST` (chat_id) — the formatter will throw if it's missing.

### 🔔 ntfy

Posts to the JSON publish API with magnitude-based priority, emoji tags, and a click-through link.  
Authentication is sent as an `Authorization: Bearer` header — the token never appears in the URL.

### 🎮 Discord

Sends a rich embed with color-coded severity, coordinates, and a direct link to the USGS event page.

### 🔧 Custom

Define your own JSON payload template in `WEBHOOK_TEMPLATE_CUSTOM` using placeholders:

```
{earthquakeId}  {magnitude}  {place}  {time}
{latitude}      {longitude}  {depth}  {url}  {timestamp}
```

Example:
```json
{"text": "🌍 M{magnitude} earthquake near {place}!", "url": "{url}"}
```

---

## ⚙️ Configuration Reference

| Variable | Required | Secret | Description |
|---|---|---|---|
| `EARTHQUAKE_API_PROVIDER` | No | No | `usgs` (default) or `seismicportal` |
| `MIN_EARTHQUAKE_MAGNITUDE` | No | No | Minimum magnitude filter (default `4.0`) |
| `EARTHQUAKE_SCHEDULE` | No | No | Cron expression (default `0 */5 * * * *`) |
| `WEBHOOK_TYPE` | Yes | No | `telegram`, `ntfy`, `discord`, `generic`, `custom` |
| `WEBHOOK_BASE_URL` | Yes | No | Server root URL (see table above) |
| `WEBHOOK_DEST` | Depends | No | Channel/topic/chat destination |
| `WEBHOOK_TOKEN` | Yes | **Yes** | Secret credential → store in Key Vault |
| `AzureWebJobsStorage` | Yes | **Yes** | Storage connection string → store in Key Vault |

---

## 🔐 Secrets Management

Sensitive settings (`WEBHOOK_TOKEN`, `AzureWebJobsStorage`) should **never** be stored in plain text in production.

### Azure Key Vault setup

```bash
# 1. Create the Key Vault
az keyvault create --name "<your-vault>" --resource-group "<rg>" --location "<region>"

# 2. Store the secret
az keyvault secret set --vault-name "<your-vault>" --name "WEBHOOK-TOKEN" --value "<token>"

# 3. Assign Managed Identity to the Function App
az functionapp identity assign --name "<func-app>" --resource-group "<rg>"

# 4. Grant read access
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee "$(az functionapp identity show --name <func-app> --resource-group <rg> --query principalId -o tsv)" \
  --scope "$(az keyvault show --name <your-vault> --query id -o tsv)"
```

Then in **Azure Portal → Function App → Environment variables**, set:

```
WEBHOOK_TOKEN = @Microsoft.KeyVault(SecretUri=https://<your-vault>.vault.azure.net/secrets/WEBHOOK-TOKEN/)
```

The Function App resolves the secret at runtime — **zero code changes required**.

---

## 🧪 Tests

```bash
dotnet test
```

66 unit tests covering:
- API client response parsing and error handling (USGS + SeismicPortal)
- Webhook formatter output for all 5 channels
- Deduplication logic
- Function orchestration and magnitude filtering

---

## ☁️ Deploy to Azure

```bash
# Publish to Azure Flex Consumption
func azure functionapp publish <your-function-app-name>
```

Recommended hosting plan: **Flex Consumption** for cost-efficient, event-driven scaling.

---

## 📦 Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| Hosting | Azure Functions v4 (Isolated Worker) |
| Storage | Azure Blob Storage |
| Secrets | Azure Key Vault + Managed Identity |
| Observability | Application Insights |
| Testing | xUnit + Moq |

---

## 📄 License

MIT
