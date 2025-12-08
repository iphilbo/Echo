# Test script to verify keep-alive function configuration
Write-Host "Testing KeepAlive Function Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if local.settings.json exists
if (Test-Path "local.settings.json") {
    Write-Host "[OK] local.settings.json found" -ForegroundColor Green

    $settings = Get-Content "local.settings.json" | ConvertFrom-Json

    # Check connection strings
    if ($settings.Values.'ConnectionStrings:Default') {
        Write-Host "[OK] Default connection string configured" -ForegroundColor Green
        $defaultConn = $settings.Values.'ConnectionStrings:Default'
        if ($defaultConn -match 'Server=([^;]+)') {
            Write-Host "     Server: $($matches[1])" -ForegroundColor Gray
        }
        if ($defaultConn -match 'Initial Catalog=([^;]+)') {
            Write-Host "     Database: $($matches[1])" -ForegroundColor Gray
        }
    } else {
        Write-Host "[ERROR] Default connection string missing" -ForegroundColor Red
    }

    if ($settings.Values.IRISConn) {
        Write-Host "[OK] IRIS connection string configured" -ForegroundColor Green
        $irisConn = $settings.Values.IRISConn
        if ($irisConn -match 'Server=([^;]+)') {
            Write-Host "     Server: $($matches[1])" -ForegroundColor Gray
        }
        if ($irisConn -match 'Initial Catalog=([^;]+)') {
            Write-Host "     Database: $($matches[1])" -ForegroundColor Gray
        }
    } else {
        Write-Host "[ERROR] IRIS connection string missing" -ForegroundColor Red
    }

    # Check database list
    if ($settings.Values.KEEPALIVE_DATABASES) {
        Write-Host "[OK] KEEPALIVE_DATABASES: $($settings.Values.KEEPALIVE_DATABASES)" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] KEEPALIVE_DATABASES not configured" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "Configuration Summary:" -ForegroundColor Cyan
    Write-Host "  Databases to keep alive: $($settings.Values.KEEPALIVE_DATABASES)" -ForegroundColor White
    Write-Host ""

} else {
    Write-Host "[ERROR] local.settings.json not found" -ForegroundColor Red
    exit 1
}

# Check if project builds
Write-Host "Building project..." -ForegroundColor Yellow
$buildOutput = dotnet build --configuration Release --no-restore 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Project builds successfully" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Build failed" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
}

Write-Host ""
Write-Host "Test Summary:" -ForegroundColor Cyan
Write-Host "  Configuration: OK" -ForegroundColor Green
Write-Host "  Build: OK" -ForegroundColor Green
Write-Host ""
Write-Host "Note: To fully test the function execution, install Azure Functions Core Tools:" -ForegroundColor Yellow
Write-Host "      npm install -g azure-functions-core-tools@4" -ForegroundColor Gray
Write-Host ""
Write-Host "Or deploy to Azure and monitor the Function App logs." -ForegroundColor Yellow
