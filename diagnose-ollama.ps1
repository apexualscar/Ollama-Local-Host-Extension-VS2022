# Ollama Connection Diagnostic Script
# Run this in PowerShell to check if Ollama is accessible

param(
    [string]$ServerAddress = "localhost"
)

Write-Host "=== Ollama Connection Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

if ($ServerAddress -ne "localhost" -and $ServerAddress -ne "127.0.0.1") {
    Write-Host "?? Remote Server Mode: Testing connection to $ServerAddress" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "?? Local Mode: Testing connection to localhost" -ForegroundColor Yellow
    Write-Host ""
}

# Check if Ollama is running (local only)
if ($ServerAddress -eq "localhost" -or $ServerAddress -eq "127.0.0.1") {
    Write-Host "1. Checking if Ollama is running locally..." -ForegroundColor Yellow
    $ollamaProcess = Get-Process -Name "ollama" -ErrorAction SilentlyContinue

    if ($ollamaProcess) {
        Write-Host "   ? Ollama process is running" -ForegroundColor Green
    } else {
        Write-Host "   ? Ollama process not found" -ForegroundColor Red
        Write-Host "   ? Start Ollama by running: ollama serve" -ForegroundColor Cyan
    }
    Write-Host ""
} else {
    # Remote server - check network connectivity
    Write-Host "1. Checking network connectivity to $ServerAddress..." -ForegroundColor Yellow
    $pingResult = Test-Connection -ComputerName $ServerAddress -Count 2 -Quiet -ErrorAction SilentlyContinue
    
    if ($pingResult) {
        Write-Host "   ? Server is reachable on network" -ForegroundColor Green
    } else {
        Write-Host "   ? Cannot reach server" -ForegroundColor Red
        Write-Host "   ? Check network connection" -ForegroundColor Cyan
        Write-Host "   ? Verify server IP address" -ForegroundColor Cyan
    }
    Write-Host ""
}

# Test HTTP connection
Write-Host "2. Testing HTTP connection to http://${ServerAddress}:11434..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "http://${ServerAddress}:11434/api/tags" -Method Get -TimeoutSec 5 -UseBasicParsing
    Write-Host "   ? Successfully connected to Ollama server" -ForegroundColor Green
    Write-Host "   ? Status Code: $($response.StatusCode)" -ForegroundColor Gray
    
    # Parse and display models
    $json = $response.Content | ConvertFrom-Json
    if ($json.models) {
        Write-Host "   ? Found $($json.models.Count) model(s):" -ForegroundColor Gray
        foreach ($model in $json.models) {
            Write-Host "      - $($model.name)" -ForegroundColor White
        }
    } else {
        Write-Host "   ? No models found. Pull a model with: ollama pull codellama" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ? Failed to connect to Ollama server" -ForegroundColor Red
    Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    
    if ($ServerAddress -eq "localhost" -or $ServerAddress -eq "127.0.0.1") {
        Write-Host "   Possible solutions (Local):" -ForegroundColor Yellow
        Write-Host "   1. Start Ollama: ollama serve" -ForegroundColor Cyan
        Write-Host "   2. Check if port 11434 is blocked by firewall" -ForegroundColor Cyan
        Write-Host "   3. Try: netstat -ano | findstr :11434" -ForegroundColor Cyan
    } else {
        Write-Host "   Possible solutions (Remote):" -ForegroundColor Yellow
        Write-Host "   1. Verify Ollama is running on server" -ForegroundColor Cyan
        Write-Host "   2. Check server is configured for network: OLLAMA_HOST=0.0.0.0:11434" -ForegroundColor Cyan
        Write-Host "   3. Check server firewall allows port 11434" -ForegroundColor Cyan
        Write-Host "   4. Verify correct IP address: $ServerAddress" -ForegroundColor Cyan
    }
}

Write-Host ""

# Check for alternative ports/addresses
if ($ServerAddress -eq "localhost" -or $ServerAddress -eq "127.0.0.1") {
    Write-Host "3. Checking for Ollama on alternative addresses..." -ForegroundColor Yellow

    $alternativeUrls = @(
        "http://127.0.0.1:11434",
        "http://localhost:11434",
        "http://0.0.0.0:11434"
    )

    foreach ($url in $alternativeUrls) {
        try {
            $response = Invoke-WebRequest -Uri "$url/api/tags" -Method Get -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
            Write-Host "   ? Found Ollama at: $url" -ForegroundColor Green
        } catch {
            Write-Host "   ? Not accessible at: $url" -ForegroundColor Gray
        }
    }
    Write-Host ""
}

# Check environment variables (local only)
if ($ServerAddress -eq "localhost" -or $ServerAddress -eq "127.0.0.1") {
    Write-Host "4. Checking Ollama environment variables..." -ForegroundColor Yellow
    $ollamaHost = [Environment]::GetEnvironmentVariable("OLLAMA_HOST", "User")
    if ($ollamaHost) {
        Write-Host "   ? OLLAMA_HOST is set to: $ollamaHost" -ForegroundColor Cyan
        Write-Host "   ? You may need to use this address in the extension settings" -ForegroundColor Yellow
    } else {
        Write-Host "   ? OLLAMA_HOST not set (using default: localhost:11434)" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
Write-Host ""

# Summary and recommendations
if ($ServerAddress -eq "localhost" -or $ServerAddress -eq "127.0.0.1") {
    Write-Host "If Ollama is not running locally, start it with:" -ForegroundColor Yellow
    Write-Host "  ollama serve" -ForegroundColor White
} else {
    Write-Host "For remote server setup:" -ForegroundColor Yellow
    Write-Host "  See REMOTE_OLLAMA_SETUP.md for detailed instructions" -ForegroundColor White
    Write-Host ""
    Write-Host "  On your server, ensure:" -ForegroundColor Yellow
    Write-Host "  1. OLLAMA_HOST=0.0.0.0:11434" -ForegroundColor White
    Write-Host "  2. ollama serve (running)" -ForegroundColor White
    Write-Host "  3. Firewall allows port 11434" -ForegroundColor White
}

Write-Host ""
Write-Host "To pull a model:" -ForegroundColor Yellow
if ($ServerAddress -eq "localhost" -or $ServerAddress -eq "127.0.0.1") {
    Write-Host "  ollama pull codellama" -ForegroundColor White
    Write-Host "  ollama pull llama3" -ForegroundColor White
} else {
    Write-Host "  (On your server) ollama pull codellama" -ForegroundColor White
    Write-Host "  (On your server) ollama pull llama3" -ForegroundColor White
}

Write-Host ""
Write-Host "?? Usage:" -ForegroundColor Cyan
Write-Host "  .\diagnose-ollama.ps1                    # Test localhost" -ForegroundColor Gray
Write-Host "  .\diagnose-ollama.ps1 -ServerAddress 192.168.1.100  # Test remote server" -ForegroundColor Gray
