# Migration Notes: Echo Project (formerly Prometheus.KeepAlive)

This document identifies information and references that need to be updated now that this project has been elevated to a standalone project.

## Critical Items to Address

### 1. **Project Name and Namespace**
   - **Current Project File**: `Prometheus.KeepAlive.csproj`
   - **Current Namespace**: `Prometheus.KeepAlive` (in `KeepAliveFunction.cs`)
   - **Action Required**:
     - Rename the `.csproj` file to match the new project name (e.g., `Echo.csproj`)
     - Update the namespace in `KeepAliveFunction.cs` to match the new project name
     - The default root namespace will automatically update when the project file is renamed

### 2. **Database Connection Information**
   - **Found in**: `bin/Release/net7.0/local.settings.json` (may exist in other build outputs)
   - **Connection String Details**:
     - Server: `prometheus-sqlserver-test.database.windows.net`
     - Database: `PrometheusDb-Dev`
     - User: `CloudSAb4d2fc1f`
     - **Note**: This connection string contains credentials and references the "Prometheus" database
   - **Action Required**:
     - Determine if this database connection should be updated to a new database
     - Update connection string references if the database has been renamed or moved
     - Verify if this database is shared with the parent project or needs to be independent


### 3. **Build Artifacts (Non-Critical)**
   - **Location**: `obj/` and `bin/` folders contain references to old path:
     - Old Path: `C:\Users\phil\source\repos\Prometheus\Prometheus.KeepAlive\`
     - New Path: `C:\Users\phil\source\repos\Intralogic\Echo\`
   - **Action Required**:
     - These are build artifacts and will be regenerated on next build
     - Consider cleaning build artifacts: `dotnet clean` or delete `bin/` and `obj/` folders
     - The build system will automatically update paths on next build

### 4. **Function Name**
   - **Current Function Name**: `DbKeepAlive`
   - **Action Required**:
     - Consider if the function name should be updated to reflect the new project identity
     - This is optional but may improve clarity

## Configuration Files Status

### ✅ Already Standalone
- `host.json` - No external references
- `local.settings.json` (root) - Minimal configuration, no external references
- `Program.cs` - No external references
- `Prometheus.KeepAlive.csproj` - Only NuGet package references (no project references)

### ⚠️ Needs Review
- `bin/Release/net7.0/local.settings.json` - Contains database connection string with "Prometheus" references
- Any other `local.settings.json` files in build outputs may contain similar references

## Dependencies

### NuGet Packages (No Changes Needed)
- Microsoft.Azure.Functions.Worker (1.22.0)
- Microsoft.Azure.Functions.Worker.Extensions.Timer (4.3.0)
- Microsoft.Azure.Functions.Worker.Sdk (1.16.0)
- Microsoft.Data.SqlClient (5.2.1)

**Status**: All dependencies are external NuGet packages, no project-to-project dependencies found.

## Database Schema Dependency

The function inserts into a `SysLog` table with the following structure:
- `LogUser` (string) - Currently hardcoded to "ChronJob"
- `LogData` (string) - Currently "Keeping Alive"

**Action Required**:
- Verify this table exists in the target database
- Confirm if this table is shared with the parent project or should be independent
- Consider if the table name or schema should be updated

## Environment Variables Used

The function uses the following environment variables (all have defaults):
- `TIME_ZONE` (default: "Eastern Standard Time")
- `WORK_DAYS` (default: "Mon-Fri")
- `WORK_START` (default: "07:00")
- `WORK_END` (default: "19:00")
- `ConnectionStrings:Default` or `SQLConn` (no default, required)

**Action Required**:
- Ensure these are configured in Azure Function App settings if deploying
- Update any documentation or deployment scripts that reference these

## Recommended Next Steps

1. **Clean build artifacts**: Run `dotnet clean` to remove old build references
2. **Rename project file**: Rename `Prometheus.KeepAlive.csproj` to `Echo.csproj` (or desired name)
3. **Update namespace**: Change `namespace Prometheus.KeepAlive` to match new project name
4. **Review database connection**: Determine if database connection string needs updating
5. **Verify database schema**: Ensure `SysLog` table exists and is accessible
6. **Update any deployment configurations**: Check Azure Function App settings, ARM templates, or deployment scripts
7. **Review function name**: Consider renaming `DbKeepAlive` if desired

## Migration Reference Items - Status

Based on the migration reference document, the following items have been restored:

### ✅ Created Files

1. **Deployment Script**: `deploy-keepalive.ps1`
   - ✅ Created with all parameters from migration reference
   - ⚠️ Note: References `verify-func-auth.ps1` which may need to be created or obtained from parent project
   - ⚠️ Note: Project file name still needs to be updated (currently uses `*.csproj` pattern)

2. **GitHub Actions Workflow**: `.github/workflows/master_keepalive.yml`
   - ✅ Created with deployment steps
   - ⚠️ Note: Updated to use .NET 8.0 (project uses 8.0, reference mentioned 6.x)
   - ⚠️ Note: Requires secret `AZUREAPPSVC_PUBLISHPROFILE_KEEPALIVE` to be configured in GitHub

3. **.gitignore**: `.gitignore`
   - ✅ Created to ignore build artifacts and publish directory
   - Includes patterns for `publish/` and `*.zip` files

### ✅ Additional Files Created

1. **Verification Script**: `verify-func-auth.ps1`
   - ✅ Created from parent project reference
   - ✅ Integrated with deployment script
   - Handles:
     - Admin endpoint verification
     - Host master key authentication
     - Base URL construction (with Azure CLI fallback)
     - DNS resolution checks
     - Unauthenticated access validation

### ⚠️ Missing/Needs Attention

2. **Solution File**: `*.sln`
   - Not present (optional)
   - **Action**: Create if using Visual Studio solution structure

3. **Azure Function App Configuration**
   - Function App Name: `KeepAlive` (from migration reference)
   - **Action**: Verify Azure Function App exists and is properly configured
   - **Action**: Configure GitHub secret `AZUREAPPSVC_PUBLISHPROFILE_KEEPALIVE` with publish profile

### 📋 Runtime Version Note

- **Migration Reference**: Mentioned .NET 6.x
- **Current Project**: Uses .NET 8.0
- **Action**: Verify Azure Function App runtime supports .NET 8.0, or consider downgrading to 6.x if needed

## ✅ Migration Complete

All migration tasks have been completed successfully:

- ✅ Project renamed to "Echo"
- ✅ Multi-database support added (Default/Prometheus and IRIS)
- ✅ GitHub Actions deployment configured and working
- ✅ Function deployed and executing successfully
- ✅ Database entries confirmed in both databases
- ✅ Documentation updated and cleaned up

### Key Fixes Applied

1. **ZIP Deployment Structure**: Fixed GitHub Actions workflow to create ZIP with files at root (not in subdirectory)
2. **Function Discovery**: Function is now properly discovered and executing
3. **Configuration**: All connection strings and settings verified and working

### Current Status

- **Function App**: KeepAlive (pro-prod-rg)
- **Runtime**: .NET 8.0 Isolated
- **Databases**: Prometheus (Default) and IRIS
- **Work Window**: Mon-Fri, 08:00-17:00 EST
- **Execution**: Every 5 minutes during work hours
- **Status**: ✅ Working and inserting entries into both databases
