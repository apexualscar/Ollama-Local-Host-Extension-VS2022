# Quick Diagnostic Script
# Run this in PowerShell to check your extension setup

Write-Host "=== Ollama Copilot Extension Diagnostics ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check if VSIX exists
Write-Host "1. Checking VSIX file..." -ForegroundColor Yellow
$vsixPath = ".\bin\Debug\OllamaLocalHostIntergration.vsix"
if (Test-Path $vsixPath) {
    $vsixInfo = Get-Item $vsixPath
    Write-Host "   ? VSIX found: $($vsixInfo.FullName)" -ForegroundColor Green
    Write-Host "   Size: $($vsixInfo.Length) bytes" -ForegroundColor Green
    Write-Host "   Modified: $($vsixInfo.LastWriteTime)" -ForegroundColor Green
} else {
    Write-Host "   ? VSIX not found! Build the project first." -ForegroundColor Red
}
Write-Host ""

# 2. Check DLL
Write-Host "2. Checking DLL file..." -ForegroundColor Yellow
$dllPath = ".\bin\Debug\OllamaLocalHostIntergration.dll"
if (Test-Path $dllPath) {
    $dllInfo = Get-Item $dllPath
    Write-Host "   ? DLL found: $($dllInfo.FullName)" -ForegroundColor Green
    Write-Host "   Size: $($dllInfo.Length) bytes" -ForegroundColor Green
} else {
    Write-Host "   ? DLL not found!" -ForegroundColor Red
}
Write-Host ""

# 3. Check Experimental Instance folder
Write-Host "3. Checking Experimental Instance..." -ForegroundColor Yellow
$expPath = "$env:LocalAppData\Microsoft\VisualStudio\17.0_*Exp"
$expFolders = Get-Item $expPath -ErrorAction SilentlyContinue
if ($expFolders) {
    Write-Host "   ? Experimental instance folder(s) found:" -ForegroundColor Green
    foreach ($folder in $expFolders) {
        Write-Host "     $($folder.FullName)" -ForegroundColor Green
    }
} else {
    Write-Host "   ! No experimental instance folder found (will be created on first debug)" -ForegroundColor Yellow
}
Write-Host ""

# 4. Check Activity Log
Write-Host "4. Checking Activity Log..." -ForegroundColor Yellow
$activityLog = "$env:AppData\Microsoft\VisualStudio\17.0_*Exp\ActivityLog.xml"
$logFiles = Get-Item $activityLog -ErrorAction SilentlyContinue
if ($logFiles) {
    Write-Host "   ? Activity log found:" -ForegroundColor Green
    foreach ($log in $logFiles) {
        Write-Host "     $($log.FullName)" -ForegroundColor Green
        $content = Get-Content $log -Raw
        if ($content -match "Ollama") {
            Write-Host "     Contains 'Ollama' references" -ForegroundColor Green
        }
        # Check for errors
        $errors = Select-String -Path $log -Pattern 'type="Error"' -CaseSensitive
        if ($errors) {
            Write-Host "     ? Found $($errors.Count) error(s) in log" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   ! No activity log yet (normal if you haven't debugged)" -ForegroundColor Yellow
}
Write-Host ""

# 5. Check VS Installation
Write-Host "5. Checking Visual Studio installation..." -ForegroundColor Yellow
$vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
if (Test-Path $vsPath) {
    Write-Host "   ? Visual Studio 2022 Community found" -ForegroundColor Green
} else {
    Write-Host "   ! Visual Studio 2022 Community not found at expected location" -ForegroundColor Yellow
    Write-Host "     Path checked: $vsPath" -ForegroundColor Gray
}
Write-Host ""

# 6. Check project file
Write-Host "6. Checking project file..." -ForegroundColor Yellow
$projPath = ".\OllamaLocalHostIntergration.csproj"
if (Test-Path $projPath) {
    Write-Host "   ? Project file found" -ForegroundColor Green
    $projContent = Get-Content $projPath -Raw
    if ($projContent -match "VsSDK") {
        Write-Host "   ? Project references VS SDK" -ForegroundColor Green
    }
} else {
    Write-Host "   ? Project file not found!" -ForegroundColor Red
}
Write-Host ""

# 7. Instructions
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To debug the extension:" -ForegroundColor White
Write-Host "  1. Press F5 in Visual Studio (not Ctrl+F5)" -ForegroundColor Gray
Write-Host "  2. Wait for Experimental Instance window to open" -ForegroundColor Gray
Write-Host "  3. In Experimental Instance: View ? Other Windows ? Ollama Copilot" -ForegroundColor Gray
Write-Host ""
Write-Host "If extension doesn't appear:" -ForegroundColor White
Write-Host "  • Check the Output window (View ? Output)" -ForegroundColor Gray
Write-Host "  • Select 'Debug' from the dropdown" -ForegroundColor Gray
Write-Host "  • Look for errors or exceptions" -ForegroundColor Gray
Write-Host ""
Write-Host "To reset Experimental Instance:" -ForegroundColor White
Write-Host "  cd 'C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE'" -ForegroundColor Gray
Write-Host "  .\devenv.exe /RootSuffix Exp /ResetSettings" -ForegroundColor Gray
Write-Host ""

# 8. Open activity log if it exists
if ($logFiles) {
    $answer = Read-Host "Open Activity Log in browser? (y/n)"
    if ($answer -eq 'y' -or $answer -eq 'Y') {
        Start-Process $logFiles[0].FullName
    }
}

Write-Host "=== Diagnostics Complete ===" -ForegroundColor Cyan
