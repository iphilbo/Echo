# GitHub Actions Setup - Azure CLI Direct Deployment ✅

This guide walks you through setting up GitHub Actions for automated deployment using Azure CLI.

**Status:** ✅ This method is tested and working!

## What You Need

You'll need to create **3 GitHub Secrets**:

1. `AZURE_CREDENTIALS` - Service principal JSON
2. `AZURE_RESOURCE_GROUP` - Your resource group name
3. `AZURE_FUNCTION_APP_NAME` - Your Function App name

## Step 1: Gather Information

First, let's get the information we need from Azure.

### Get Your Subscription ID

```powershell
az account show --query id -o tsv
```

**Save this value** - you'll need it in the next step.

### Get Your Resource Group Name

```powershell
az group list --query "[].name" -o table
```

Or if you know it:
```powershell
az functionapp show --name "YourFunctionAppName" --query "resourceGroup" -o tsv
```

**Save this value** - this is your `AZURE_RESOURCE_GROUP`.

### Get Your Function App Name

This is the name of your Azure Function App (e.g., "KeepAlive").

**Save this value** - this is your `AZURE_FUNCTION_APP_NAME`.

## Step 2: Create Service Principal

Run this command (replace the placeholders with your actual values):

```powershell
az ad sp create-for-rbac --name "github-actions-echo" --role contributor --scopes /subscriptions/{YOUR-SUBSCRIPTION-ID}/resourceGroups/{YOUR-RESOURCE-GROUP-NAME} --sdk-auth
```

**Example:**
```powershell
az ad sp create-for-rbac --name "github-actions-echo" --role contributor --scopes /subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/MyResourceGroup --sdk-auth
```

**This will output JSON like:**
```json
{
  "clientId": "abcd1234-5678-90ef-ghij-klmnopqrstuv",
  "clientSecret": "wxyz9876-5432-10fe-dcba-zyxwvutsrqpo",
  "subscriptionId": "12345678-1234-1234-1234-123456789012",
  "tenantId": "87654321-4321-4321-4321-210987654321",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

**⚠️ IMPORTANT:** Copy the ENTIRE JSON output - you'll need it in the next step!

## Step 3: Add GitHub Secrets

Go to your GitHub repository: https://github.com/iphilbo/Echo

### Secret 1: AZURE_CREDENTIALS

1. Navigate to: **Settings** → **Secrets and variables** → **Actions**
2. Click **"New repository secret"**
3. **Name**: `AZURE_CREDENTIALS`
4. **Value**: Paste the **ENTIRE JSON** from Step 2 (the whole thing, including all the curly braces)
5. Click **"Add secret"**

### Secret 2: AZURE_RESOURCE_GROUP

1. Click **"New repository secret"** again
2. **Name**: `AZURE_RESOURCE_GROUP`
3. **Value**: Your resource group name (e.g., `MyResourceGroup`)
4. Click **"Add secret"**

### Secret 3: AZURE_FUNCTION_APP_NAME

1. Click **"New repository secret"** again
2. **Name**: `AZURE_FUNCTION_APP_NAME`
3. **Value**: Your Function App name (e.g., `KeepAlive`)
4. Click **"Add secret"**

## Step 4: Verify Secrets

You should now have 3 secrets:
- ✅ `AZURE_CREDENTIALS`
- ✅ `AZURE_RESOURCE_GROUP`
- ✅ `AZURE_FUNCTION_APP_NAME`

## Step 5: Test the Deployment

1. Go to the **Actions** tab in your GitHub repository
2. Click **"Deploy Echo KeepAlive Function App"** workflow
3. Click **"Run workflow"** → **"Run workflow"**
4. Watch it deploy!

## Troubleshooting

### Error: "Service principal not found"
- Make sure you ran the `az ad sp create-for-rbac` command successfully
- Check that the JSON was copied completely

### Error: "Resource group not found"
- Verify `AZURE_RESOURCE_GROUP` secret matches your actual resource group name
- Check spelling and case sensitivity

### Error: "Function App not found"
- Verify `AZURE_FUNCTION_APP_NAME` secret matches your actual Function App name
- Check spelling and case sensitivity

### Error: "Insufficient permissions"
- The service principal needs Contributor role on the resource group
- Re-run the `az ad sp create-for-rbac` command with the correct scope

## Quick Reference

**Commands to run:**
```powershell
# Get subscription ID
az account show --query id -o tsv

# Get resource group (if you know Function App name)
az functionapp show --name "KeepAlive" --query "resourceGroup" -o tsv

# Create service principal (replace placeholders)
az ad sp create-for-rbac --name "github-actions-echo" --role contributor --scopes /subscriptions/{SUBSCRIPTION-ID}/resourceGroups/{RESOURCE-GROUP} --sdk-auth
```

**GitHub Secrets needed:**
- `AZURE_CREDENTIALS` = Full JSON from service principal creation
- `AZURE_RESOURCE_GROUP` = Your resource group name
- `AZURE_FUNCTION_APP_NAME` = Your Function App name
