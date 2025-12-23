# Echo KeepAlive Azure Function

An Azure Functions application that prevents Azure SQL Database from auto-shutting down during business hours by performing periodic write operations.

**Repository:** https://github.com/iphilbo/Echo

## Overview

This Azure Function App runs a timer-triggered function that periodically connects to Azure SQL Databases and calls application heartbeat endpoints to keep them warm during business hours. The primary purpose is to **prevent auto-shutdown during business hours**, which saves costs by allowing resources to shut down outside business hours.

### Why This Exists

Azure SQL Database (especially serverless tiers) can be configured to auto-pause or shut down after periods of inactivity to reduce costs. However, when the database is shut down, the first connection after inactivity experiences a "cold start" delay (typically 5-30 seconds), which can impact application performance.

This function:
- **Prevents auto-shutdown during business hours**: Keeps databases and application layers active when they're needed
- **Allows shutdown outside business hours**: Saves costs by not keeping resources warm 24/7
- **Minimal overhead**: Simple INSERT operation and HTTP GET request every 5 minutes during work hours
- **Cost-optimized**: Only runs when necessary (business hours only)

## How It Works

### Function Execution

The `DbKeepAlive` function is triggered by an Azure Functions Timer Trigger that runs:
- **Schedule**: Every 5 minutes (`0 */5 * * * *` - cron expression)
- **Run on Startup**: Yes (executes immediately when the function app starts)

### Work Window Logic

The function only performs the keep-alive operation during configured business hours:

1. **Time Zone**: Configurable (default: "Eastern Standard Time")
2. **Work Days**: Configurable (default: Monday-Friday)
3. **Work Hours**: Configurable start and end times (default: 07:00-19:00)

If the current time is outside the work window, the function logs a skip message and returns without connecting to the database.

### Database and Application Operations

When within the work window, the function performs keep-alive operations for:

#### Database Keep-Alive

For each configured database:

1. Retrieves the database connection string from environment variables
2. Opens a connection to the Azure SQL Database
3. Executes a simple INSERT statement to keep the database active:
   ```sql
   INSERT INTO SysLog (LogUser, LogData)
   VALUES ('ChronJob', 'Keeping Alive')
   ```
4. Logs the operation result

**Note**: The INSERT operation serves two purposes:
- **Prevents auto-shutdown**: The write activity keeps the database from being considered "idle"
- **Minimal impact**: Uses an existing `SysLog` table, creating minimal overhead

#### Application Heartbeat

For each configured heartbeat endpoint:

1. Retrieves the heartbeat URLs from environment variables (default: both IRIS and Dev endpoints)
2. Makes an HTTP GET request to each heartbeat endpoint
3. Logs the operation result for each endpoint

**Note**: The heartbeat calls serve to:
- **Prevent app layer cold starts**: Keeps the applications warm during business hours
- **Minimal overhead**: Simple HTTP GET request with no payload
- **Multiple endpoints**: Supports comma-separated list of URLs to keep multiple apps warm

All operations (databases and heartbeat) are executed in parallel for efficiency.


### Error Handling

- Database connection failures are caught and logged as errors
- HTTP request failures (heartbeat) are caught and logged as errors
- Missing connection strings are logged as warnings
- All exceptions are handled gracefully to prevent function crashes
- If one operation fails, other operations (other databases, heartbeat) continue to execute

## Configuration

### Environment Variables

The function uses the following environment variables (all have defaults):

| Variable | Default | Description |
|----------|---------|-------------|
| `KEEPALIVE_DATABASES` | `"Default"` | Comma-separated list of database names to keep alive (e.g., "Default,Secondary,Third") |
| `ConnectionStrings:Default` or `SQLConn` | *(required for "Default")* | Primary SQL Server connection string |
| `ConnectionStrings:{DatabaseName}` | *(required per database)* | Connection string for each database listed in `KEEPALIVE_DATABASES` |
| `HEARTBEAT_URL` | `"https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat"` | Comma-separated list of application heartbeat endpoint URLs to keep app layer warm |
| `TIME_ZONE` | `"Eastern Standard Time"` | Time zone identifier for work window calculations |
| `WORK_DAYS` | `"Mon-Fri"` | Comma-separated or hyphenated day range (e.g., "Mon-Fri", "Mon,Wed,Fri", "Sat,Sun") |
| `WORK_START` | `"07:00"` | Start time of work window (24-hour format, HH:mm) |
| `WORK_END` | `"19:00"` | End time of work window (24-hour format, HH:mm) |

### Multiple Database Support

The function can keep multiple databases warm simultaneously. To configure multiple databases:

1. **Set the database list**: Configure `KEEPALIVE_DATABASES` with comma-separated database names:
   ```
   KEEPALIVE_DATABASES=Default,Secondary,Third
   ```

2. **Add connection strings**: For each database name, add a connection string:
   - `ConnectionStrings:Default` or `SQLConn` - for the "Default" database
   - `IRISConn` - for the "IRIS" database (special case)
   - `ConnectionStrings:{DatabaseName}` - for other databases

**Example Configuration:**
```json
{
  "Values": {
    "KEEPALIVE_DATABASES": "Default,Secondary",
    "ConnectionStrings:Default": "Server=...;Database=corp-db;...",
    "ConnectionStrings:Secondary": "Server=...;Database=secondary-db;..."
  }
}
```

**Note**: All databases are processed in parallel for efficiency. If one database fails, the others will still be processed.

### Multiple Heartbeat Endpoint Support

The function can keep multiple application heartbeat endpoints warm simultaneously. By default, it calls both IRIS and Dev endpoints:

- **Default configuration**: `https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat`

To customize the heartbeat endpoints:

1. **Set the heartbeat URL list**: Configure `HEARTBEAT_URL` with comma-separated URLs:
   ```
   HEARTBEAT_URL=https://app1.com/api/heartbeat,https://app2.com/api/heartbeat,https://app3.com/api/heartbeat
   ```

2. **Single endpoint**: To use only one endpoint, provide a single URL:
   ```
   HEARTBEAT_URL=https://iris.intralogichealth.com/api/heartbeat
   ```

**Note**: All heartbeat endpoints are processed in parallel for efficiency. If one endpoint fails, the others will still be processed.

### Connection String Lookup

The function looks up connection strings based on database name:

- **Default database**: Checks in order:
  1. `ConnectionStrings:Default`
  2. `SQLConn` (backward compatibility)

- **IRIS database**: Uses `IRISConn` environment variable

- **Other databases**: Uses `ConnectionStrings:{DatabaseName}` format

If a connection string is not found, the function logs a warning and skips that database.

### Work Days Format

The `WORK_DAYS` variable accepts:
- **Range format**: `"Mon-Fri"` (Monday through Friday)
- **Individual days**: `"Mon,Wed,Fri"` (specific weekdays)
- **Weekend**: `"Sat,Sun"` or `"Sat-Sun"`
- **Case-insensitive**: Any combination of day abbreviations

### Database Schema Requirement

The function requires a `SysLog` table with the following structure:

```sql
CREATE TABLE SysLog (
    LogUser NVARCHAR(255),
    LogData NVARCHAR(MAX)
    -- Additional columns may exist but are not used by this function
)
```

## Architecture

### Technology Stack

- **.NET 8.0**: Target framework
- **Azure Functions v4**: Runtime version
- **.NET Isolated Worker Process**: Execution model
- **Microsoft.Data.SqlClient**: SQL Server connectivity
- **Timer Trigger**: Azure Functions extension for scheduled execution

### Project Structure

```
.
├── KeepAliveFunction.cs      # Main function implementation
├── Program.cs                # Function app host configuration
├── host.json                 # Azure Functions host configuration
├── local.settings.json       # Local development settings (gitignored)
├── Prometheus.KeepAlive.csproj  # Project file
├── deploy-keepalive.ps1      # Deployment script
├── verify-func-auth.ps1      # Deployment verification script
└── .github/
    └── workflows/
        └── master_keepalive.yml  # CI/CD pipeline
```

## Development

> **📖 For complete setup instructions on a new machine, see [SETUP.md](SETUP.md)**

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local) (optional, for local testing)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for deployment)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/iphilbo/Echo.git
   cd Echo
   ```

2. **Configure local settings**

   Copy `local.settings.json.template` to `local.settings.json` and configure:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "HEARTBEAT_URL": "https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat",
       "TIME_ZONE": "Eastern Standard Time",
       "WORK_DAYS": "Mon-Fri",
       "WORK_START": "07:00",
       "WORK_END": "19:00"
     }
   }
   ```

3. **Run locally**
   ```bash
   func start
   ```

   The function will execute every 5 minutes, or you can trigger it manually via the Azure Functions admin endpoint.

### Building

```bash
dotnet build
```

### Testing

The function can be tested by:
1. Running locally and observing logs
2. Adjusting `WORK_START` and `WORK_END` to include current time
3. Verifying heartbeat endpoint calls in the logs
4. Manually testing heartbeat endpoints to ensure they're accessible

## Deployment

### Prerequisites

- Azure subscription
- Azure Function App created
- Resource Group with appropriate permissions
- GitHub repository with secrets configured (for CI/CD)

### Manual Deployment

Use the PowerShell deployment script:

```powershell
.\deploy-keepalive.ps1 `
    -ResourceGroup "MyResourceGroup" `
    -FunctionAppName "KeepAlive" `
    -Tail
```

**Parameters:**
- `-ResourceGroup` (required): Azure Resource Group name
- `-FunctionAppName` (required): Azure Function App name
- `-Output`: Output directory (default: `./publish/keepalive`)
- `-SkipPublish`: Skip publish if output exists
- `-Tail`: Tail logs after deployment
- `-BaseUrl`: Base URL for verification
- `-AllowDirty`: Allow deployment with uncommitted changes
- `-AutoCommitMessage`: Auto-commit with message
- `-Push`: Push after auto-commit
- `-SkipBuildCheck`: Skip build validation
- `-Slot`: Deployment slot name

### Automated Deployment (CI/CD)

The project includes a GitHub Actions workflow (`.github/workflows/master_keepalive.yml`) that:

1. Triggers on push to `master` or `main` branch
2. Builds and publishes the project
3. Creates a ZIP package
4. Deploys to Azure Function App using Azure CLI

**Required GitHub Secrets:**
- `AZURE_CREDENTIALS`: Service principal JSON (created via `az ad sp create-for-rbac`)
- `AZURE_RESOURCE_GROUP`: Resource group name (e.g., `pro-prod-rg`)
- `AZURE_FUNCTION_APP_NAME`: Function App name (e.g., `KeepAlive`)

See `GITHUB_ACTIONS_SETUP.md` for detailed setup instructions.

### Azure Function App Configuration

After deployment, configure the following Application Settings in the Azure Portal:

1. **Connection Strings** (one per database):
   - Name: `ConnectionStrings:Default`
     - Type: SQLAzure (or Custom)
     - Value: Your primary SQL Server connection string
   - Name: `ConnectionStrings:Secondary` (if using multiple databases)
     - Type: SQLAzure (or Custom)
     - Value: Your secondary database connection string
   - Add additional `ConnectionStrings:{DatabaseName}` for each database

2. **Environment Variables**:
   - `KEEPALIVE_DATABASES`: `"Default"` (or `"Default,Secondary"` for multiple databases)
   - `TIME_ZONE`: `"Eastern Standard Time"` (optional, defaults shown)
   - `WORK_DAYS`: `"Mon-Fri"` (optional)
   - `WORK_START`: `"07:00"` (optional)
   - `WORK_END`: `"19:00"` (optional)

### Verification

After deployment, the script automatically verifies the deployment by:
1. Retrieving the Function App master key
2. Testing unauthenticated access (should be blocked)
3. Testing authenticated access with master key (should succeed)

## Monitoring

### Logs

The function logs the following information:

- **Skip events**: When outside work window (includes current time, timezone, and window settings)
- **Success**: When keep-alive insert succeeds (includes rows affected and timestamp)
- **Warnings**: Missing connection strings
- **Errors**: Connection failures or other exceptions

### Azure Monitor

Monitor the function through:
- **Application Insights**: If configured, view function execution metrics
- **Function App Logs**: Stream logs in real-time
- **Metrics**: Function execution count, duration, errors

### Database Monitoring

Check the `SysLog` table for periodic entries:
- `LogUser`: `"ChronJob"`
- `LogData`: `"Keeping Alive"`
- Timestamp: When the entry was created (if your table has a timestamp column)

## Troubleshooting

### Database Still Auto-Shutting Down

1. **Verify function is running**: Check Function App logs to ensure the function executes during business hours
2. **Check work window**: Verify current time is within `WORK_START` and `WORK_END`
3. **Check work days**: Verify current day matches `WORK_DAYS` configuration
4. **Check timezone**: Verify `TIME_ZONE` is correct for your region
5. **Verify database tier**: Ensure your Azure SQL Database tier supports auto-pause (serverless tiers)
6. **Check function execution frequency**: Ensure the 5-minute interval is frequent enough for your database's auto-pause delay setting

### Function Not Executing

1. **Check work window**: Verify current time is within `WORK_START` and `WORK_END`
2. **Check work days**: Verify current day matches `WORK_DAYS` configuration
3. **Check timezone**: Verify `TIME_ZONE` is correct for your region
4. **Check function app status**: Ensure Function App is running in Azure Portal

### Database Connection Failures

1. **Verify connection string**: Check `ConnectionStrings:Default` in Azure Portal
2. **Test connectivity**: Ensure Function App can reach SQL Server (firewall rules, VNet configuration)
3. **Check credentials**: Verify SQL Server authentication credentials
4. **Review logs**: Check Application Insights or Function App logs for detailed error messages

### Missing Log Entries

1. **Verify function is discovered:**
   ```powershell
   az functionapp function show --resource-group "YourResourceGroup" --name "KeepAlive" --function-name "DbKeepAlive"
   ```
   - If function is not found, the ZIP deployment structure is incorrect (files must be at root, not in subdirectory)

2. **Verify table exists**: Ensure `SysLog` table exists in the target database
3. **Check permissions**: Verify the SQL user has INSERT permissions
4. **Review function logs**: Check for errors in Application Insights
5. **Check configuration**: Verify `KEEPALIVE_DATABASES` and connection strings are set correctly

## Security Considerations

- **Connection Strings**: Stored securely in Azure Function App settings (encrypted at rest)
- **Database Credentials**: Use Azure Key Vault for production environments
- **Network Security**: Consider VNet integration for private database access
- **Function App Authentication**: Admin endpoints require master key authentication

## License

[Specify your license here]

## Support

For issues, questions, or contributions, please [create an issue](link-to-issues) or contact the development team.
