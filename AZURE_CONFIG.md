# Azure Configuration

This document contains configuration details for Azure deployment.

## Azure Function App Configuration

Configure the following in your Azure Function App's **Configuration** → **Application settings**:

### Application Settings

**Required:**
- `HEARTBEAT_URL`: Comma-separated list of heartbeat endpoint URLs
  - Default: `https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat`
  - Example: `https://app1.com/api/heartbeat,https://app2.com/api/heartbeat`

**Optional (defaults shown):**
- `TIME_ZONE`: `Eastern Standard Time`
- `WORK_DAYS`: `Mon-Fri`
- `WORK_START`: `07:00`
- `WORK_END`: `19:00`

### Configuration Steps

1. **Navigate to Azure Portal:**
   - Go to your Function App
   - Click **Configuration** → **Application settings**

2. **Add/Update Settings:**
   - Click **+ New application setting** for each setting
   - Enter the name and value
   - Click **OK**

3. **Save Configuration:**
   - Click **Save** at the top
   - Restart the Function App if prompted

### Example Configuration

```
HEARTBEAT_URL = https://iris.intralogichealth.com/api/heartbeat,https://dev.intralogichealth.com/api/heartbeat
TIME_ZONE = Eastern Standard Time
WORK_DAYS = Mon-Fri
WORK_START = 07:00
WORK_END = 19:00
```

## Security Notes

⚠️ **IMPORTANT**:
- Heartbeat URLs are public endpoints and do not contain sensitive credentials
- If heartbeat endpoints require authentication, 401 responses are treated as warnings (server is still considered warm)
- For production environments, consider using Azure Key Vault for any sensitive configuration

## Verification

After configuration:

1. **Check Function App logs:**
   - Navigate to Function App → Functions → `HeartbeatKeepAlive` → Monitor
   - Look for successful heartbeat calls during business hours

2. **Expected behavior:**
   - Function runs every 5 minutes during configured work window
   - Makes HTTP GET requests to all configured heartbeat endpoints
   - Logs show successful calls or warnings for 401 responses

## Troubleshooting

**Heartbeat endpoints not being called:**
- Verify `HEARTBEAT_URL` is set correctly (comma-separated, no spaces unless intentional)
- Check work window settings (current time must be within `WORK_START` and `WORK_END`)
- Verify `WORK_DAYS` includes the current day
- Check Function App is running

**401 Unauthorized responses:**
- This is expected if endpoints require authentication
- The function treats 401 as a warning (server is still warm)
- Verify endpoints are accessible manually

**Other HTTP errors:**
- Check network connectivity from Function App
- Verify URLs are correct and accessible
- Review Application Insights logs for detailed error messages
