# Deployment Guide

This guide explains how to deploy the KeepAlive Azure Function App to Azure.

## Prerequisites

1. **Azure CLI** installed and logged in:
   ```powershell
   az login
   az account set --subscription "Your-Subscription-Name"
   ```

2. **Azure Function App** created in Azure Portal
   - Runtime: .NET 8.0 (Isolated)
   - Function App name: `KeepAlive` (or your preferred name)

3. **Resource Group** with the Function App

## Deployment Methods

### Method 1: PowerShell Script (Recommended for Manual Deployment)

Use the `deploy-keepalive.ps1` script for manual deployments.

#### Basic Deployment

```powershell
.\deploy-keepalive.ps1 `
    -ResourceGroup "YourResourceGroup" `
    -FunctionAppName "KeepAlive"
```

#### With Log Tailing

```powershell
.\deploy-keepalive.ps1 `
    -ResourceGroup "YourResourceGroup" `
    -FunctionAppName "KeepAlive" `
    -Tail
```

#### All Parameters

```powershell
.\deploy-keepalive.ps1 `
    -ResourceGroup "YourResourceGroup" `
    -FunctionAppName "KeepAlive" `
    -Output "./publish/keepalive" `
    -Tail `
    -AllowDirty
```

**Parameters:**
- `-ResourceGroup` (required): Azure Resource Group name
- `-FunctionAppName` (required): Azure Function App name
- `-Output`: Output directory (default: `./publish/keepalive`)
- `-Tail`: Tail logs after deployment
- `-SkipPublish`: Skip publish if output exists
- `-AllowDirty`: Allow deployment with uncommitted changes
- `-BaseUrl`: Base URL for verification
- `-AutoCommitMessage`: Auto-commit with message
- `-Push`: Push after auto-commit
- `-SkipBuildCheck`: Skip build validation
- `-Slot`: Deployment slot name

### Method 2: GitHub Actions (Automated CI/CD)

The project includes a GitHub Actions workflow that automatically deploys on push to `master` or `main`.

#### Setup

1. **Configure GitHub Secret:**
   - Go to your GitHub repository
   - Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `AZUREAPPSVC_PUBLISHPROFILE_KEEPALIVE`
   - Value: Download the publish profile from Azure Portal:
     - Function App → Overview → Get publish profile
     - Copy the entire XML content

2. **Push to trigger deployment:**
   ```bash
   git push origin master
   ```

The workflow will:
- Build the project
- Create a ZIP package
- Deploy to Azure Function App

#### Manual Workflow Trigger

You can also trigger the workflow manually:
- Go to GitHub repository → Actions tab
- Select "Deploy KeepAlive Function App"
- Click "Run workflow"

### Method 3: Azure Portal (Manual Upload)

1. **Build and publish locally:**
   ```powershell
   dotnet publish --configuration Release --output ./publish/keepalive
   ```

2. **Create ZIP:**
   ```powershell
   Compress-Archive -Path "./publish/keepalive/*" -DestinationPath "./publish/keepalive.zip" -Force
   ```

3. **Deploy via Azure Portal:**
   - Function App → Deployment Center
   - Choose "Zip Deploy"
   - Upload `keepalive.zip`

## Post-Deployment Configuration

After deploying, configure the connection strings and settings in Azure Portal:

### 1. Configure Connection Strings

**Option A: Connection Strings Section (Recommended)**
- Function App → Configuration → Connection strings
- Click "+ New connection string"
- **Name**: `Default`
- **Value**: Your Prometheus connection string
- **Type**: SQLAzure
- Click "OK"
- Repeat for IRIS:
  - **Name**: `IRIS` (or use Application Setting `IRISConn`)

**Option B: Application Settings**
- Function App → Configuration → Application settings
- Click "+ New application setting"
- **Name**: `ConnectionStrings:Default`
- **Value**: Your Prometheus connection string
- Click "OK"
- Add `IRISConn`:
  - **Name**: `IRISConn`
  - **Value**: Your IRIS connection string

### 2. Configure Application Settings

Add these application settings:
- **Name**: `KEEPALIVE_DATABASES`
- **Value**: `Default,IRIS`

Optional settings (defaults shown):
- `TIME_ZONE`: `Eastern Standard Time`
- `WORK_DAYS`: `Mon-Fri`
- `WORK_START`: `07:00`
- `WORK_END`: `19:00`

### 3. Save Configuration

Click "Save" at the top of the Configuration page. The Function App will restart.

## Verify Deployment

### Check Function App Status

1. **Azure Portal:**
   - Function App → Overview
   - Verify status is "Running"

2. **Function Logs:**
   - Function App → Functions → `DbKeepAlive` → Monitor
   - Check for execution logs

3. **Application Insights (if configured):**
   - View function execution metrics and logs

### Test Database Connections

The deployment script includes automatic verification. You can also manually verify:

1. **Check Logs:**
   ```powershell
   az functionapp log tail --resource-group "YourResourceGroup" --name "KeepAlive"
   ```

2. **Verify Database Inserts:**
   - Connect to Prometheus database
   - Query: `SELECT TOP 10 * FROM SysLog WHERE LogUser = 'ChronJob' ORDER BY LogDate DESC`
   - Repeat for IRIS database

## Troubleshooting

### Deployment Fails

1. **Check Azure CLI login:**
   ```powershell
   az account show
   ```

2. **Verify permissions:**
   - Ensure you have Contributor or Owner role on the Resource Group

3. **Check Function App exists:**
   ```powershell
   az functionapp show --resource-group "YourResourceGroup" --name "KeepAlive"
   ```

### Function Not Executing

1. **Check configuration:**
   - Verify connection strings are set correctly
   - Check `KEEPALIVE_DATABASES` setting

2. **Check work window:**
   - Function only runs during business hours (Mon-Fri, 7am-7pm by default)
   - Adjust `WORK_START` and `WORK_END` if needed

3. **Check logs:**
   - Function App → Functions → `DbKeepAlive` → Monitor
   - Look for errors or warnings

### Connection String Issues

1. **Verify connection strings:**
   - Check they're correctly formatted
   - Ensure credentials are valid
   - Verify firewall rules allow Function App access

2. **Check environment variable names:**
   - Default: `ConnectionStrings:Default` or `SQLConn`
   - IRIS: `IRISConn` (not `ConnectionStrings:IRIS`)

## Quick Reference

### Connection Strings Needed

**Prometheus (Default):**
- Name: `ConnectionStrings:Default` (in Connection Strings section) OR `ConnectionStrings:Default` (in Application Settings)
- Value: `Server=tcp:prometheus-sqlserver-test.database.windows.net,1433;Initial Catalog=PrometheusDb-Dev;...`

**IRIS:**
- Name: `IRISConn` (in Application Settings)
- Value: `Server=tcp:sqlserver-corp.database.windows.net,1433;Initial Catalog=corp-db;...`

### Application Settings Needed

- `KEEPALIVE_DATABASES`: `Default,IRIS`
- `TIME_ZONE`: `Eastern Standard Time` (optional)
- `WORK_DAYS`: `Mon-Fri` (optional)
- `WORK_START`: `07:00` (optional)
- `WORK_END`: `19:00` (optional)

## Next Steps

After successful deployment:
1. Monitor the function execution in Azure Portal
2. Verify database inserts in both `SysLog` tables
3. Adjust work window settings if needed
4. Set up alerts for function failures (optional)
