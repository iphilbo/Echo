# How to Check Logs for IRIS Database Errors

If IRIS database keep-alive is failing, here's how to find the error messages.

## Where Errors Are Logged

The function logs errors using `_logger.LogError()` which sends them to:
1. **Azure Function App Logs** (in Azure Portal)
2. **Application Insights** (if configured)
3. **Function App Stream Logs** (real-time)

## Error Scenarios and Log Messages

### 1. Missing Connection String

**Error Location:** Line 100-108 in `KeepAliveFunction.cs`

**Log Message:**
```
[Warning] No connection string found for database 'IRIS' (expected: IRISConn)
```

**What to check:**
- Azure Portal → Function App → Configuration → Application settings
- Verify `IRISConn` exists and has a value
- Check spelling (must be exactly `IRISConn`, case-sensitive)

### 2. Database Connection Failure

**Error Location:** Line 121-123 in `KeepAliveFunction.cs`

**Log Message:**
```
[Error] Keep-alive failed for database 'IRIS'
```

**Exception details will include:**
- Connection timeout
- Authentication failure
- Network/firewall issues
- Invalid server/database name

**Common causes:**
- Firewall blocking Function App IP
- Invalid credentials
- Server name incorrect
- Database doesn't exist

### 3. SQL Execution Error

**Error Location:** Line 121-123 (caught during INSERT)

**Log Message:**
```
[Error] Keep-alive failed for database 'IRIS'
```

**Exception details will include:**
- Table doesn't exist: `Invalid object name 'SysLog'`
- Permission denied: `INSERT permission denied`
- Schema mismatch

## How to Check Logs

### Method 1: Azure Portal (Easiest)

1. Go to Azure Portal
2. Navigate to your Function App: **KeepAlive**
3. Go to **Functions** → **DbKeepAlive**
4. Click **Monitor** tab
5. Look for recent executions
6. Click on a failed execution to see logs
7. Look for:
   - Red error entries
   - Yellow warning entries
   - Messages containing "IRIS"

### Method 2: Application Insights (If Configured)

1. Go to Azure Portal
2. Navigate to your Function App
3. Click **Application Insights** (or go to the App Insights resource)
4. Go to **Logs** or **Failures**
5. Query for:
   ```
   traces
   | where message contains "IRIS"
   | order by timestamp desc
   ```

### Method 3: Azure CLI (Real-time)

```powershell
az functionapp log tail --resource-group "pro-prod-rg" --name "KeepAlive"
```

This shows real-time logs. Look for:
- `[Error] Keep-alive failed for database 'IRIS'`
- `[Warning] No connection string found for database 'IRIS'`

### Method 4: Log Stream in Portal

1. Azure Portal → Function App → **Log stream**
2. Shows real-time logs
3. Filter for "IRIS" or "Error"

## What to Look For

### Successful Execution
```
[Information] Processing keep-alive for 2 database(s): Default, IRIS
[Information] Database 'Default' keep-alive successful. RowsAffected=1 at 2025-01-XX...
[Information] Database 'IRIS' keep-alive successful. RowsAffected=1 at 2025-01-XX...
```

### Missing Connection String
```
[Warning] No connection string found for database 'IRIS' (expected: IRISConn)
```

### Connection Failure
```
[Error] Keep-alive failed for database 'IRIS'
System.Data.SqlClient.SqlException: A network-related or instance-specific error occurred...
```

### Table Missing
```
[Error] Keep-alive failed for database 'IRIS'
System.Data.SqlClient.SqlException: Invalid object name 'SysLog'
```

## Quick Diagnostic Steps

1. **Check if function is running:**
   - Azure Portal → Functions → DbKeepAlive → Monitor
   - Verify executions are happening

2. **Check for IRIS-specific errors:**
   - Look for log entries containing "IRIS"
   - Check for Error or Warning level

3. **Verify configuration:**
   - Function App → Configuration
   - Check `IRISConn` exists
   - Check `KEEPALIVE_DATABASES` = `Default,IRIS`

4. **Test connection manually:**
   - Try connecting to IRIS database from your machine
   - Verify the connection string works
   - Check firewall rules

## Example Log Queries

### Find all IRIS-related errors:
```
traces
| where message contains "IRIS" and severityLevel >= 3
| order by timestamp desc
```

### Find recent failures:
```
exceptions
| where outerMessage contains "IRIS"
| order by timestamp desc
```

## Next Steps After Finding Error

1. **If connection string missing:**
   - Add `IRISConn` to Application Settings in Azure Portal

2. **If connection fails:**
   - Verify connection string is correct
   - Check firewall rules
   - Test connection manually

3. **If table missing:**
   - Run `create_syslog_table.sql` on IRIS database

4. **If permission denied:**
   - Grant INSERT permission on SysLog table to the database user
