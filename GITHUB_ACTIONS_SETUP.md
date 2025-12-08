# GitHub Actions Setup Guide

This guide explains how to set up GitHub Actions for automated deployment.

## Option 1: Using Azure Service Principal (Recommended)

This method uses Azure service principal credentials, which is more reliable than publish profiles.

### Step 1: Create Azure Service Principal

Run this command in Azure CLI (or PowerShell with Azure CLI):

```powershell
az ad sp create-for-rbac --name "github-actions-echo" --role contributor --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group-name} --sdk-auth
```

Replace:
- `{subscription-id}`: Your Azure subscription ID
- `{resource-group-name}`: Your resource group name (e.g., the one containing your Function App)

**Output will look like:**
```json
{
  "clientId": "xxxx-xxxx-xxxx",
  "clientSecret": "xxxx-xxxx-xxxx",
  "subscriptionId": "xxxx-xxxx-xxxx",
  "tenantId": "xxxx-xxxx-xxxx",
  ...
}
```

### Step 2: Add GitHub Secret

1. Go to your GitHub repository: https://github.com/iphilbo/Echo
2. Navigate to: **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. **Name**: `AZURE_CREDENTIALS`
5. **Value**: Paste the **entire JSON output** from Step 1
6. Click **"Add secret"**

### Step 3: Update Function App Name (if needed)

If your Function App name is not "KeepAlive", update the workflow file:
- Edit `.github/workflows/master_keepalive.yml`
- Change `app-name: 'KeepAlive'` to your actual Function App name

## Option 2: Using Publish Profile (Alternative)

If you prefer to use publish profile, follow these steps:

### Step 1: Get Fresh Publish Profile

1. Go to Azure Portal
2. Navigate to your Function App
3. Click **"Overview"** in the left menu
4. Click **"Get publish profile"** button (downloads an XML file)
5. Open the XML file and copy **ALL** the content

### Step 2: Add GitHub Secret

1. Go to your GitHub repository: https://github.com/iphilbo/Echo
2. Navigate to: **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. **Name**: `AZUREAPPSVC_PUBLISHPROFILE_KEEPALIVE`
5. **Value**: Paste the **entire XML content** from the publish profile
6. Click **"Add secret"**

### Step 3: Update Workflow (if using publish profile)

If you want to use publish profile instead of service principal, update the workflow:

```yaml
- name: Deploy to Azure Function App
  uses: Azure/functions-action@v1
  with:
    app-name: 'KeepAlive'
    publish-profile: ${{ secrets.AZUREAPPSVC_PUBLISHPROFILE_KEEPALIVE }}
    package: './publish/keepalive.zip'
```

## Verify Setup

1. Go to **Actions** tab in your GitHub repository
2. Click **"Deploy Echo KeepAlive Function App"**
3. Click **"Run workflow"** → **"Run workflow"**
4. Monitor the workflow execution

## Troubleshooting

### Error: 401 Unauthorized

**If using publish profile:**
- The publish profile may be expired or invalid
- Get a fresh publish profile from Azure Portal
- Make sure you copied the ENTIRE XML content

**If using service principal:**
- Verify the service principal has Contributor role on the resource group
- Check that the JSON secret is correctly formatted
- Ensure subscription ID and resource group are correct

### Error: Function App not found

- Verify the Function App name in the workflow matches your Azure Function App
- Check that the Function App exists in the specified resource group
- Ensure the service principal has access to the resource group

### Error: Deployment failed

- Check Function App logs in Azure Portal
- Verify the ZIP package was created successfully
- Ensure the Function App runtime is set to .NET 8.0 (Isolated)

## Required Permissions

The service principal needs:
- **Contributor** role on the Resource Group (or Function App)
- Access to the Azure subscription

## Security Notes

- Never commit secrets to the repository
- Rotate service principal credentials periodically
- Use least privilege principle (only grant necessary permissions)

