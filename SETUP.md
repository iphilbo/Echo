# Setup and Configuration Guide

Complete guide for setting up the Echo KeepAlive Azure Function on a new machine.

## Prerequisites

Before you begin, ensure you have the following installed:

### Required Software

1. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation:
     ```powershell
     dotnet --version
     ```
     Should show version 8.0.x or higher

2. **Azure Functions Core Tools** (optional, for local testing)
   - Install via npm:
     ```powershell
     npm install -g azure-functions-core-tools@4 --unsafe-perm true
     ```
   - Or via Chocolatey:
     ```powershell
     choco install azure-functions-core-tools-4
     ```
   - Verify installation:
     ```powershell
     func --version
     ```

3. **Azure CLI** (for deployment and Azure management)
   - Download: https://docs.microsoft.com/cli/azure/install-azure-cli
   - Verify installation:
     ```powershell
     az --version
     ```

4. **Git** (for version control)
   - Download: https://git-scm.com/downloads
   - Verify installation:
     ```powershell
     git --version
     ```

5. **PowerShell 7+** (recommended, but PowerShell 5.1+ works)
   - Download: https://github.com/PowerShell/PowerShell/releases
   - Verify installation:
     ```powershell
     $PSVersionTable.PSVersion
     ```

### Optional Tools

- **Visual Studio 2022** or **Visual Studio Code** (with C# extension)
- **SQL Server Management Studio (SSMS)** or **Azure Data Studio** (for database management)

## Initial Setup

### 1. Clone the Repository

```powershell
# Navigate to your desired workspace directory
cd C:\Users\<YourUsername>\source\repos

# Clone the repository
git clone https://github.com/iphilbo/Echo.git

# Navigate into the project directory
cd Echo
```

### 2. Verify Project Structure

You should see the following key files:
- `Prometheus.KeepAlive.csproj` - Project file
- `KeepAliveFunction.cs` - Main function code
- `Program.cs` - Function app host
- `host.json` - Azure Functions configuration
- `local.settings.json.template` - Template for local settings
- `README.md` - Project documentation

### 3. Restore NuGet Packages

```powershell
dotnet restore
```

This will download all required NuGet packages defined in `Prometheus.KeepAlive.csproj`.

### 4. Build the Project

```powershell
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Local Development Configuration

### 1. Create Local Settings File

The `local.settings.json` file is gitignored and contains your local development configuration.

**Copy the template:**
```powershell
Copy-Item local.settings.json.template local.settings.json
```

### 2. Configure Local Settings

Edit `local.settings.json` with your configuration:

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

**Configuration Options:**

- `HEARTBEAT_URL`: Comma-separated list of heartbeat endpoint URLs to keep warm
  - Default: Both IRIS and Dev endpoints
  - Example: `"https://app1.com/api/heartbeat,https://app2.com/api/heartbeat"`
  
- `TIME_ZONE`: Time zone for work window calculations
  - Default: `"Eastern Standard Time"`
  - Common values: `"Eastern Standard Time"`, `"Central Standard Time"`, `"Pacific Standard Time"`, `"UTC"`
  
- `WORK_DAYS`: Days when keep-alive should run
  - Default: `"Mon-Fri"`
  - Options: `"Mon-Fri"`, `"Mon,Wed,Fri"`, `"Sat,Sun"`, etc.
  
- `WORK_START`: Start time of work window (24-hour format)
  - Default: `"07:00"`
  - Format: `"HH:mm"` (e.g., `"09:00"`, `"08:30"`)
  
- `WORK_END`: End time of work window (24-hour format)
  - Default: `"19:00"`
  - Format: `"HH:mm"` (e.g., `"17:00"`, `"18:30"`)

**Note:** `local.settings.json` is gitignored and will not be committed to the repository.

### 3. Test Local Configuration

Run the test script to verify your setup:

```powershell
.\TestKeepAlive.ps1
```

This script will:
- Verify .NET SDK is installed
- Check project builds successfully
- Validate `local.settings.json` exists
- Check configuration values

**Note:** If Azure Functions Core Tools are not installed, the script will skip the `func` command test.

## Running Locally

### Option 1: Using Azure Functions Core Tools

If you have Azure Functions Core Tools installed:

```powershell
func start
```

The function will:
- Start the local Functions runtime
- Execute every 5 minutes (or on startup)
- Only run during configured work window
- Log output to the console

**To test immediately:**
- Adjust `WORK_START` and `WORK_END` in `local.settings.json` to include the current time
- Or manually trigger via the Functions admin endpoint

### Option 2: Using .NET Run

You can also run the function directly:

```powershell
dotnet run
```

This will start the function app, but you'll need Azure Functions Core Tools for full local development experience.

## Building for Production

### Build Release Version

```powershell
dotnet build --configuration Release
```

Output will be in: `bin/Release/net8.0/`

### Publish for Deployment

```powershell
dotnet publish --configuration Release --output ./publish/keepalive
```

This creates a deployment-ready package in `./publish/keepalive/`.

## Azure Configuration

### 1. Login to Azure CLI

```powershell
az login
```

This will open a browser for authentication.

### 2. Set Default Subscription (Optional)

```powershell
# List available subscriptions
az account list --output table

# Set default subscription
az account set --subscription "<Subscription-Name-or-ID>"
```

### 3. Verify Function App Access

```powershell
# List function apps in resource group
az functionapp list --resource-group "<ResourceGroupName>" --output table

# Get function app details
az functionapp show --resource-group "<ResourceGroupName>" --name "<FunctionAppName>"
```

## Environment-Specific Configuration

### Development Machine

- Use `local.settings.json` for local testing
- No connection strings needed (heartbeat-only)
- Can test with different work windows

### Azure Function App

After deployment, configure these Application Settings in Azure Portal:

1. **Navigate to:** Function App → Configuration → Application settings

2. **Add/Update Settings:**
   - `HEARTBEAT_URL`: `https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat`
   - `TIME_ZONE`: `Eastern Standard Time`
   - `WORK_DAYS`: `Mon-Fri`
   - `WORK_START`: `07:00`
   - `WORK_END`: `19:00`

3. **Save and Restart:**
   - Click "Save"
   - Restart the Function App if prompted

## Verification Checklist

After setup, verify:

- [ ] .NET 8.0 SDK installed and working
- [ ] Repository cloned successfully
- [ ] Project builds without errors (`dotnet build`)
- [ ] `local.settings.json` created and configured
- [ ] Test script runs successfully (`.\TestKeepAlive.ps1`)
- [ ] Azure CLI installed and logged in
- [ ] Can access Azure Function App (if deployed)

## Troubleshooting

### Build Errors

**Error: `The target framework 'net8.0' is not installed`**
- Solution: Install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

**Error: `NU1101: Unable to find package`**
- Solution: Run `dotnet restore` to restore NuGet packages
- Check internet connection
- Verify NuGet package sources are configured

### Runtime Errors

**Error: `func: The term 'func' is not recognized`**
- Solution: Install Azure Functions Core Tools (see Prerequisites)
- Or use `dotnet run` instead

**Error: `Cannot find local.settings.json`**
- Solution: Copy `local.settings.json.template` to `local.settings.json`
- Ensure file is in the project root directory

### Configuration Issues

**Function not running during expected hours**
- Check `WORK_START` and `WORK_END` times
- Verify `TIME_ZONE` is correct
- Check `WORK_DAYS` includes current day
- Remember: Times are in 24-hour format (e.g., `19:00` = 7:00 PM)

**Heartbeat endpoints failing**
- Verify URLs are correct and accessible
- Check network connectivity
- Review function logs for detailed error messages

## Next Steps

After completing setup:

1. **Review Documentation:**
   - `README.md` - Full project documentation
   - `QUICK_START.md` - Quick reference guide
   - `DEPLOYMENT.md` - Deployment procedures

2. **Test Locally:**
   - Run `func start` to test the function
   - Adjust work window to test immediately
   - Check logs for successful heartbeat calls

3. **Deploy to Azure:**
   - See `DEPLOYMENT.md` for deployment options
   - Use GitHub Actions for automated deployment
   - Or use `deploy-keepalive.ps1` for manual deployment

## Additional Resources

- **Azure Functions Documentation:** https://docs.microsoft.com/azure/azure-functions/
- **.NET 8.0 Documentation:** https://docs.microsoft.com/dotnet/
- **Azure CLI Documentation:** https://docs.microsoft.com/cli/azure/
- **Repository:** https://github.com/iphilbo/Echo

## Support

For issues or questions:
- Check existing documentation files
- Review function logs in Azure Portal
- Create an issue in the GitHub repository

