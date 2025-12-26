# Phase 5.1 Diagnostic Script
# Checks AI model connection to verify Ollama and Qwen setup

Write-Host "?? Phase 5.1: AI Model Connection Diagnostic" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if Ollama is running
Write-Host "?? Step 1: Checking Ollama Service..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 5
    Write-Host "? Ollama is running at localhost:11434" -ForegroundColor Green
    
    # Parse models
    $models = ($response.Content | ConvertFrom-Json).models
    Write-Host "   Found $($models.Count) model(s):" -ForegroundColor Gray
    
    $hasQwen = $false
    foreach ($model in $models) {
        $modelName = $model.name
        Write-Host "   ?? $modelName" -ForegroundColor White
        if ($modelName -match "qwen") {
            $hasQwen = $true
            Write-Host "      ? Qwen model found!" -ForegroundColor Green
        }
    }
    
    if (-not $hasQwen) {
        Write-Host ""
        Write-Host "??  WARNING: No Qwen model found!" -ForegroundColor Yellow
        Write-Host "   Available models listed above." -ForegroundColor Yellow
        Write-Host "   To install Qwen, run:" -ForegroundColor Yellow
        Write-Host "   ollama pull qwen2" -ForegroundColor White
    }
}
catch {
    Write-Host "? Cannot connect to Ollama at localhost:11434" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? Solutions:" -ForegroundColor Yellow
    Write-Host "   1. Start Ollama: ollama serve" -ForegroundColor White
    Write-Host "   2. Check if Ollama is installed" -ForegroundColor White
    Write-Host "   3. Verify firewall isn't blocking port 11434" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Step 2: Check extension configuration
Write-Host "?? Step 2: Checking Extension Configuration..." -ForegroundColor Yellow

$settingsPath = "$env:APPDATA\OllamaVSExtension\settings.json"
if (Test-Path $settingsPath) {
    Write-Host "? Settings file found" -ForegroundColor Green
    Write-Host "   Location: $settingsPath" -ForegroundColor Gray
    
    $settings = Get-Content $settingsPath | ConvertFrom-Json
    Write-Host "   Server: $($settings.ServerAddress)" -ForegroundColor White
    Write-Host "   Model: $($settings.SelectedModel)" -ForegroundColor White
    
    # Verify server matches
    if ($settings.ServerAddress -eq "http://localhost:11434") {
        Write-Host "   ? Server address is correct" -ForegroundColor Green
    } else {
        Write-Host "   ??  Server address is: $($settings.ServerAddress)" -ForegroundColor Yellow
        Write-Host "   Should be: http://localhost:11434" -ForegroundColor Yellow
    }
    
    # Check if model is Qwen
    if ($settings.SelectedModel -match "qwen") {
        Write-Host "   ? Qwen model is selected" -ForegroundColor Green
    } else {
        Write-Host "   ??  Current model: $($settings.SelectedModel)" -ForegroundColor Yellow
        Write-Host "   Recommendation: Select a Qwen model" -ForegroundColor Yellow
    }
} else {
    Write-Host "??  Settings file not found" -ForegroundColor Yellow
    Write-Host "   This is normal for first run" -ForegroundColor Gray
    Write-Host "   Settings will be created when you use the extension" -ForegroundColor Gray
}

Write-Host ""
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Step 3: Test actual connection
Write-Host "?? Step 3: Testing Model Response..." -ForegroundColor Yellow

$testModel = $models[0].name
Write-Host "   Using model: $testModel" -ForegroundColor Gray

try {
    $testRequest = @{
        model = $testModel
        messages = @(
            @{
                role = "user"
                content = "Say 'Hello from Ollama' in exactly 3 words"
            }
        )
        stream = $false
    } | ConvertTo-Json -Depth 10
    
    Write-Host "   Sending test message..." -ForegroundColor Gray
    $testResponse = Invoke-WebRequest -Uri "http://localhost:11434/api/chat" `
        -Method Post `
        -Body $testRequest `
        -ContentType "application/json" `
        -TimeoutSec 30
    
    $result = ($testResponse.Content | ConvertFrom-Json).message.content
    Write-Host "   ? Response received: $result" -ForegroundColor Green
    Write-Host ""
    Write-Host "   ? Ollama is working correctly!" -ForegroundColor Green
}
catch {
    Write-Host "   ? Test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Step 4: Final recommendations
Write-Host "?? Step 4: Recommendations" -ForegroundColor Yellow
Write-Host ""

if ($hasQwen) {
    Write-Host "? Qwen is installed and available" -ForegroundColor Green
    Write-Host "   In Visual Studio extension:" -ForegroundColor White
    Write-Host "   1. Click ? Settings button" -ForegroundColor White
    Write-Host "   2. Verify Server: http://localhost:11434" -ForegroundColor White
    Write-Host "   3. Select Qwen from Model dropdown" -ForegroundColor White
    Write-Host "   4. Close settings and test with a message" -ForegroundColor White
} else {
    Write-Host "??  Qwen not found" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   To install Qwen:" -ForegroundColor White
    Write-Host "   1. Open terminal" -ForegroundColor White
    Write-Host "   2. Run: ollama pull qwen2" -ForegroundColor White
    Write-Host "   3. Wait for download to complete" -ForegroundColor White
    Write-Host "   4. Restart this diagnostic" -ForegroundColor White
    Write-Host ""
    Write-Host "   Alternative models available:" -ForegroundColor Gray
    foreach ($model in $models) {
        Write-Host "   - $($model.name)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?? Diagnostic Complete!" -ForegroundColor Green
Write-Host ""
