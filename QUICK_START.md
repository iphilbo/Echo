# Quick Start Guide

Get the Echo KeepAlive Function App up and running quickly.

## Prerequisites

- Azure subscription
- Azure Function App created (Runtime: .NET 8.0 Isolated)
- Azure CLI installed and logged in
- .NET 8.0 SDK installed
- GitHub repository: https://github.com/iphilbo/Echo

## Quick Setup Steps

### 1. Local Development

1. **Clone the repository:**
   ```bash
   git clone https://github.com/iphilbo/Echo.git
   cd Echo
   ```

2. **Configure local settings:**
   - Copy `local.settings.json.template` to `local.settings.json`
   - Configure heartbeat endpoints (default includes IRIS and Dev):
     - `HEARTBEAT_URL`: Comma-separated list of URLs
     - Example: `"https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat"`
   - Optional: Adjust work window settings:
     - `TIME_ZONE`: `"Eastern Standard Time"`
     - `WORK_DAYS`: `"Mon-Fri"`
     - `WORK_START`: `"07:00"`
     - `WORK_END`: `"19:00"`

3. **Test locally:**
   ```powershell
   dotnet build
   func start  # If you have Azure Functions Core Tools
   ```

### 2. GitHub Actions Setup (Automated Deployment)

1. **Create service principal:**
   ```powershell
   az ad sp create-for-rbac --name "github-actions-echo" --role contributor --scopes /subscriptions/{SUBSCRIPTION-ID}/resourceGroups/{RESOURCE-GROUP} --sdk-auth
   ```

2. **Add GitHub Secrets:**
   - Go to: https://github.com/iphilbo/Echo/settings/secrets/actions
   - Add `AZURE_CREDENTIALS` (JSON from step 1)
   - Add `AZURE_RESOURCE_GROUP` (e.g., `pro-prod-rg`)
   - Add `AZURE_FUNCTION_APP_NAME` (e.g., `KeepAlive`)

3. **Deploy:**
   - Push to `main` branch, or
   - Go to Actions tab → Run workflow manually

### 3. Azure Configuration

After deployment, configure in Azure Portal:

**Application Settings:**
- `HEARTBEAT_URL` = `https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat`
- `TIME_ZONE` = `Eastern Standard Time` (optional, default shown)
- `WORK_DAYS` = `Mon-Fri` (optional, default shown)
- `WORK_START` = `07:00` (optional, default shown)
- `WORK_END` = `19:00` (optional, default shown)

## Verify It's Working

1. **Check Function App logs:**
   - Azure Portal → Function App → Functions → `HeartbeatKeepAlive` → Monitor
   - Look for successful heartbeat calls during business hours

2. **Expected behavior:**
   - Function runs every 5 minutes during business hours (Mon-Fri, 7am-7pm EST)
   - Makes HTTP GET requests to configured heartbeat endpoints
   - Logs show successful heartbeat calls
   - Skips execution outside business hours

3. **Check heartbeat endpoints:**
   - Manually test endpoints to verify they're responding
   - Review function logs for any HTTP errors

## Troubleshooting

**Function not running?**
- Check work window (Mon-Fri, 7am-7pm by default)
- Verify `HEARTBEAT_URL` is configured correctly
- Check Function App is running
- Verify current time is within work window

**Heartbeat endpoints failing?**
- Verify URLs are correct and accessible
- Check network connectivity from Function App
- Review function logs for detailed error messages
- Note: 401 Unauthorized is treated as a warning (server is still warm)

**Deployment issues?**
- See `DEPLOYMENT.md` for detailed deployment guide
- Check GitHub Actions logs for errors
- Verify all 3 GitHub secrets are set correctly

## Next Steps

- Review `SETUP.md` for complete setup instructions
- Review `README.md` for full documentation
- See `DEPLOYMENT.md` for deployment options
- Check `GITHUB_ACTIONS_SETUP.md` for CI/CD setup details
