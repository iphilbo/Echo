param(
    [Parameter(Mandatory = $true)]
    [string]$FunctionAppName,

    [Parameter(Mandatory = $true)]
    [string]$MasterKey,

    [string]$BaseUrl,

    [string]$ResourceGroup,

    [int]$TimeoutSec = 20
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Resolve base URL
if (-not $BaseUrl) {
    # Try Azure CLI first if a resource group is provided
    if ($ResourceGroup) {
        try {
            $defaultHost = az functionapp show --resource-group $ResourceGroup --name $FunctionAppName --query "defaultHostName" -o tsv 2>$null
            if ($LASTEXITCODE -eq 0 -and $defaultHost) { $BaseUrl = "https://$defaultHost" }
        }
        catch { }
    }
    if (-not $BaseUrl) { $BaseUrl = "https://$FunctionAppName.azurewebsites.net" }
}

$adminStatusUrl = "$BaseUrl/admin/host/status"

Write-Host "Function App: $FunctionAppName" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "Endpoint: $adminStatusUrl" -ForegroundColor Cyan

# Quick DNS/port check to catch resolution issues early
try {
    $uri = [System.Uri]$BaseUrl
    $hostName = $uri.Host
    $dns = Resolve-DnsName -Name $hostName -ErrorAction SilentlyContinue
    if (-not $dns) { Write-Warning "DNS lookup failed for $hostName. If you're in a sovereign cloud, pass -BaseUrl (e.g., https://$FunctionAppName.azurewebsites.us)." }
}
catch { Write-Warning "Unable to parse or resolve BaseUrl: $($_.Exception.Message)" }

# 1) Unauthenticated call should be rejected
Write-Host "`nChecking unauthenticated access (should be 401/403) ..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Method GET -Uri $adminStatusUrl -TimeoutSec $TimeoutSec | Out-Null
    Write-Warning "Unexpected: unauthenticated call succeeded. Verify that admin endpoints are not publicly open."
}
catch {
    $status = try { $_.Exception.Response.StatusCode.value__ } catch { "Unknown" }
    $msg = $_.Exception.Message
    Write-Host "Unauthenticated blocked as expected. Status: $status" -ForegroundColor Green
    if ($status -eq "Unknown") { Write-Host "Detail: $msg" -ForegroundColor DarkGray }
}

# 2) Authenticated call with host _master key should succeed
Write-Host "`nChecking authenticated access with host master key ..." -ForegroundColor Yellow
$headers = @{ "x-functions-key" = $MasterKey }
try {
    $response = Invoke-RestMethod -Method GET -Uri $adminStatusUrl -Headers $headers -TimeoutSec $TimeoutSec
    Write-Host "Authenticated OK." -ForegroundColor Green
    $response | ConvertTo-Json -Depth 6
}
catch {
    $status = try { $_.Exception.Response.StatusCode.value__ } catch { "Unknown" }
    Write-Error "Authenticated call failed. Status: $status  Message: $($_.Exception.Message)"
}

Write-Host "`nDone." -ForegroundColor Cyan


