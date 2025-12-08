<#
.SYNOPSIS
    Deploys the KeepAlive Azure Function App.

.DESCRIPTION
    This script builds, packages, and deploys the KeepAlive Azure Function App to Azure.
    It performs build validation, creates a ZIP package, and deploys using Azure CLI.

.PARAMETER ResourceGroup
    The Azure Resource Group name. (Mandatory)

.PARAMETER FunctionAppName
    The Azure Function App name. (Mandatory)

.PARAMETER Output
    The output directory for published files. Default: ./publish/keepalive

.PARAMETER SkipPublish
    Skip the publish step if output directory already exists.

.PARAMETER Tail
    Tail the function app logs after deployment.

.PARAMETER BaseUrl
    Base URL for function app verification.

.PARAMETER AllowDirty
    Allow deployment with uncommitted changes.

.PARAMETER AutoCommitMessage
    Automatically commit changes with this message.

.PARAMETER Push
    Push changes after auto-commit.

.PARAMETER SkipBuildCheck
    Skip build validation checks.

.PARAMETER Slot
    Deployment slot name (optional).

.EXAMPLE
    .\deploy-keepalive.ps1 -ResourceGroup "MyResourceGroup" -FunctionAppName "KeepAlive"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$FunctionAppName,

    [string]$Output = "./publish/keepalive",

    [switch]$SkipPublish,

    [switch]$Tail,

    [string]$BaseUrl,

    [switch]$AllowDirty,

    [switch]$AutoCommitMessage,

    [switch]$Push,

    [switch]$SkipBuildCheck,

    [string]$Slot
)

$ErrorActionPreference = "Stop"

# Get the project file
$projectFile = Get-ChildItem -Filter "*.csproj" | Select-Object -First 1
if (-not $projectFile) {
    Write-Error "No .csproj file found in current directory"
    exit 1
}

Write-Host "Deploying KeepAlive Function App: $FunctionAppName" -ForegroundColor Cyan
Write-Host "Project: $($projectFile.Name)" -ForegroundColor Gray

# Build validation
if (-not $SkipBuildCheck) {
    Write-Host "`nValidating build..." -ForegroundColor Yellow
    dotnet build $projectFile.FullName --configuration Release --warnaserror
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build validation failed"
        exit 1
    }
    Write-Host "Build validation passed" -ForegroundColor Green
}

# Git working tree check
if (-not $AllowDirty) {
    $gitStatus = git status --porcelain
    if ($gitStatus) {
        Write-Warning "Working tree has uncommitted changes:"
        Write-Host $gitStatus
        Write-Error "Deployment aborted. Use -AllowDirty to override or commit changes first."
        exit 1
    }
}

# Auto-commit if requested
if ($AutoCommitMessage) {
    Write-Host "`nAuto-committing changes..." -ForegroundColor Yellow
    git add -A
    git commit -m $AutoCommitMessage
    if ($Push) {
        git push
    }
}

# Publish project
if (-not $SkipPublish -or -not (Test-Path $Output)) {
    Write-Host "`nPublishing project to $Output..." -ForegroundColor Yellow
    if (Test-Path $Output) {
        Remove-Item $Output -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Output -Force | Out-Null

    dotnet publish $projectFile.FullName --configuration Release --output $Output
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed"
        exit 1
    }
    Write-Host "Publish completed" -ForegroundColor Green
} else {
    Write-Host "`nSkipping publish (output directory exists)" -ForegroundColor Gray
}

# Create ZIP package
$zipPath = "./publish/keepalive.zip"
Write-Host "`nCreating ZIP package: $zipPath..." -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Ensure publish directory exists
$publishDir = Split-Path $zipPath -Parent
if (-not (Test-Path $publishDir)) {
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
}

Compress-Archive -Path "$Output/*" -DestinationPath $zipPath -Force
Write-Host "ZIP package created: $zipPath ($([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB)" -ForegroundColor Green

# Deploy to Azure
Write-Host "`nDeploying to Azure Function App: $FunctionAppName..." -ForegroundColor Yellow
$deployParams = @(
    "functionapp", "deployment", "source", "config-zip",
    "--resource-group", $ResourceGroup,
    "--name", $FunctionAppName,
    "--src", (Resolve-Path $zipPath).Path
)

if ($Slot) {
    $deployParams += "--slot", $Slot
}

az @deployParams
if ($LASTEXITCODE -ne 0) {
    Write-Error "Azure deployment failed"
    exit 1
}

Write-Host "Deployment completed successfully" -ForegroundColor Green

# Verification (if verify-func-auth.ps1 exists)
$verifyScript = Join-Path $PSScriptRoot "verify-func-auth.ps1"
if (Test-Path $verifyScript) {
    Write-Host "`nVerifying deployment..." -ForegroundColor Yellow
    try {
        # Retrieve the host master key
        $masterKeyParams = @(
            "functionapp", "keys", "list",
            "--resource-group", $ResourceGroup,
            "--name", $FunctionAppName,
            "--query", "functionKeys._master",
            "--output", "tsv"
        )
        if ($Slot) {
            $masterKeyParams += "--slot", $Slot
        }
        $masterKey = az @masterKeyParams
        if ($LASTEXITCODE -eq 0 -and $masterKey) {
            $verifyParams = @{
                ResourceGroup = $ResourceGroup
                FunctionAppName = $FunctionAppName
                MasterKey = $masterKey.Trim()
            }
            if ($BaseUrl) {
                $verifyParams.BaseUrl = $BaseUrl
            }
            & $verifyScript @verifyParams
        } else {
            Write-Warning "Could not retrieve master key for verification. Skipping verification."
        }
    }
    catch {
        Write-Warning "Verification failed: $($_.Exception.Message)"
    }
}

# Tail logs if requested
if ($Tail) {
    Write-Host "`nTailing function app logs..." -ForegroundColor Yellow
    $logParams = @(
        "functionapp", "log", "tail",
        "--resource-group", $ResourceGroup,
        "--name", $FunctionAppName
    )
    if ($Slot) {
        $logParams += "--slot", $Slot
    }
    az @logParams
}

Write-Host "`nDeployment complete!" -ForegroundColor Green
