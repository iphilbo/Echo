# Quick Start Guide

Get the Echo KeepAlive Function App up and running quickly.

## Prerequisites

- Azure subscription
- Azure Function App created (Runtime: .NET 8.0 Isolated)
- Azure CLI installed and logged in
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
   - Add your connection strings:
     - `ConnectionStrings:Default` - Prometheus database
     - `IRISConn` - IRIS database
   - Set `KEEPALIVE_DATABASES`: `Default,IRIS`

3. **Test locally:**
   ```powershell
   dotnet build
   func start  # If you have Azure Functions Core Tools
   ```

### 2. Database Setup

Create the `SysLog` table in both databases:

**Prometheus Database:**
```sql
CREATE TABLE SysLog (
    LogUser NVARCHAR(255),
    LogData NVARCHAR(MAX)
);
```

**IRIS Database:**
```sql
CREATE TABLE SysLog (
    LogUser NVARCHAR(255),
    LogData NVARCHAR(MAX)
);
```

See `create_syslog_table.sql` or `create_syslog_table_minimal.sql` for full scripts.

### 3. GitHub Actions Setup (Automated Deployment)

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

### 4. Azure Configuration

After deployment, configure in Azure Portal:

**Connection Strings:**
- `ConnectionStrings:Default` → Prometheus connection string
- `IRISConn` → IRIS connection string

**Application Settings:**
- `KEEPALIVE_DATABASES` = `Default,IRIS`
- `TIME_ZONE` = `Eastern Standard Time` (optional)
- `WORK_DAYS` = `Mon-Fri` (optional)
- `WORK_START` = `07:00` (optional)
- `WORK_END` = `19:00` (optional)

## Verify It's Working

1. **Check Function App logs:**
   - Azure Portal → Function App → Functions → `DbKeepAlive` → Monitor

2. **Check database inserts:**
   ```sql
   SELECT TOP 10 * FROM SysLog
   WHERE LogUser = 'ChronJob'
   ORDER BY LogDate DESC
   ```

3. **Expected behavior:**
   - Function runs every 5 minutes during business hours
   - Inserts "Keeping Alive" entries into both databases
   - Logs show successful executions

## Troubleshooting

**Function not running?**
- Check work window (Mon-Fri, 7am-7pm by default)
- Verify connection strings are configured
- Check Function App is running

**Database connection errors?**
- Verify firewall rules allow Function App access
- Check connection string credentials
- Ensure `SysLog` table exists

**Deployment issues?**
- See `DEPLOYMENT.md` for detailed deployment guide
- Check GitHub Actions logs for errors
- Verify all 3 GitHub secrets are set correctly

## Next Steps

- Review `README.md` for full documentation
- See `DEPLOYMENT.md` for deployment options
- Check `GITHUB_ACTIONS_SETUP.md` for CI/CD setup details
