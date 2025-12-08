# How to Add a New Database to Keep-Alive

This guide explains how to add additional databases to the keep-alive process.

## Quick Steps

1. **Add the connection string** to your configuration:
   - **Local**: Add to `local.settings.json`
   - **Azure**: Add to Function App Application Settings

2. **Update the database list** in `KEEPALIVE_DATABASES` environment variable

3. **Deploy** (if using Azure)

## Detailed Instructions

### Step 1: Add Connection String

**For Local Development (`local.settings.json`):**
```json
{
  "Values": {
    "ConnectionStrings:YourDatabaseName": "Server=...;Database=...;..."
  }
}
```

**For Azure (Function App → Configuration → Application settings):**
- Click "+ New application setting"
- **Name**: `ConnectionStrings:YourDatabaseName` (or use a custom name like `IRISConn` for IRIS)
- **Type**: Application Setting (or Connection String if using Connection Strings section)
- **Value**: Your connection string
- Click "OK" and "Save"

   **Note**: IRIS database uses `IRISConn` instead of `ConnectionStrings:IRIS`

### Step 2: Update Database List

**For Local Development (`local.settings.json`):**
```json
{
  "Values": {
    "KEEPALIVE_DATABASES": "Default,YourDatabaseName"
  }
}
```

**For Azure (Function App → Configuration → Application settings):**
- Find or create `KEEPALIVE_DATABASES`
- **Value**: `Default,YourDatabaseName` (comma-separated list)
- Click "OK" and "Save"

### Step 3: Verify

The function will now process all databases listed in `KEEPALIVE_DATABASES`. Check the logs to verify:

```
Processing keep-alive for 2 database(s): Default, YourDatabaseName
Database 'Default' keep-alive successful...
Database 'YourDatabaseName' keep-alive successful...
```

## Example: Adding a "Secondary" Database

### Configuration

**local.settings.json:**
```json
{
  "Values": {
    "KEEPALIVE_DATABASES": "Default,Secondary",
    "ConnectionStrings:Default": "Server=tcp:sqlserver-corp.database.windows.net,1433;Initial Catalog=corp-db;...",
    "ConnectionStrings:Secondary": "Server=tcp:sqlserver-secondary.database.windows.net,1433;Initial Catalog=secondary-db;..."
  }
}
```

### Azure Portal Configuration

1. **Connection String 1:**
   - Name: `ConnectionStrings:Default`
   - Value: `Server=tcp:sqlserver-corp...`

2. **Connection String 2:**
   - Name: `ConnectionStrings:Secondary`
   - Value: `Server=tcp:sqlserver-secondary...`

3. **Application Setting:**
   - Name: `KEEPALIVE_DATABASES`
   - Value: `Default,Secondary`

## Notes

- **Database names are case-sensitive** in the connection string key
- **All databases are processed in parallel** for efficiency
- **If one database fails**, others will still be processed
- **The "Default" database** can use `ConnectionStrings:Default` or `SQLConn` (for backward compatibility)
- **Other databases** must use the format `ConnectionStrings:{DatabaseName}`

## Troubleshooting

**Database not being processed:**
- Verify the database name in `KEEPALIVE_DATABASES` matches the connection string key (after `ConnectionStrings:`)
- Check logs for warnings about missing connection strings
- Ensure the connection string is valid and accessible

**Connection failures:**
- Verify firewall rules allow the Function App to access the database
- Check connection string credentials
- Review Application Insights logs for detailed error messages
