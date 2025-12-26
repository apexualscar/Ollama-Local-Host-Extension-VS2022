# Comprehensive script to organize ALL documentation files
# Moves all .md files to docs/ except README.md

Write-Host "?? Comprehensive Documentation Organization" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Create docs directory if it doesn't exist
$docsPath = "docs"
if (-not (Test-Path $docsPath)) {
    New-Item -Path $docsPath -ItemType Directory | Out-Null
    Write-Host "? Created 'docs' folder" -ForegroundColor Green
} else {
    Write-Host "?? 'docs' folder exists" -ForegroundColor Yellow
}

Write-Host ""

# List of files to move (from your open files list)
$filesToMove = @(
    "PHASE_2_COMPLETE.md",
    "SETTINGS_FEATURE_COMPLETE.md",
    "SETTINGS_IMPLEMENTATION_COMPLETE.md",
    "SETTINGS_QUICK_START.md",
    "SETTINGS_PERSISTENCE.md",
    "STYLING_FIX_COMPLETE.md",
    "FIX_BUILD_NOW.md",
    "BUILD_FIX_INSTRUCTIONS.md",
    "UI_MODEL_DISPLAY_UPDATE.md",
    "DEBUG_FETCH_MODELS.md",
    "TROUBLESHOOTING_FLOWCHART.md",
    "EXTENSION_NOT_DETECTING.md",
    "PROJECT_COMPLETE.md",
    "PHASE_3_COMPLETE.md",
    "PHASE_2_1_SUMMARY.md",
    "STYLING_FIXES.md",
    "UI_VISUAL_COMPARISON.md",
    "UI_COPILOT_STYLE_UPDATE.md",
    "UI_UPDATE_SUMMARY.md",
    "VERIFICATION.md",
    "START_HERE.md",
    "TROUBLESHOOTING.md",
    "TESTING_GUIDE.md"
)

Write-Host "?? Moving documentation files..." -ForegroundColor Cyan
Write-Host ""

$movedCount = 0
$alreadyInDocs = 0
$notFound = 0

foreach ($fileName in $filesToMove) {
    $sourcePath = Join-Path "." $fileName
    $destPath = Join-Path $docsPath $fileName
    
    # Check if file exists in root
    if (Test-Path $sourcePath) {
        # Check if already in docs
        if (Test-Path $destPath) {
            Write-Host "??  Already in docs: $fileName" -ForegroundColor Yellow
            $alreadyInDocs++
        } else {
            try {
                Move-Item -Path $sourcePath -Destination $destPath -Force
                Write-Host "? Moved: $fileName" -ForegroundColor Green
                $movedCount++
            } catch {
                Write-Host "? Failed: $fileName - $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } elseif (Test-Path $destPath) {
        # Already moved previously
        Write-Host "?? Already in docs: $fileName" -ForegroundColor Gray
        $alreadyInDocs++
    } else {
        Write-Host "? Not found: $fileName" -ForegroundColor DarkGray
        $notFound++
    }
}

# Also move any other .md files that might exist
Write-Host ""
Write-Host "?? Checking for additional .md files..." -ForegroundColor Cyan
$additionalFiles = Get-ChildItem -Path "." -Filter "*.md" -File | Where-Object {
    $_.Name -ne "README.md" -and $filesToMove -notcontains $_.Name
}

if ($additionalFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "Found $($additionalFiles.Count) additional file(s):" -ForegroundColor Yellow
    foreach ($file in $additionalFiles) {
        $destPath = Join-Path $docsPath $file.Name
        if (-not (Test-Path $destPath)) {
            Move-Item -Path $file.FullName -Destination $destPath -Force
            Write-Host "? Moved: $($file.Name)" -ForegroundColor Green
            $movedCount++
        }
    }
}

Write-Host ""
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?? Summary:" -ForegroundColor Cyan
Write-Host "   ? Moved: $movedCount files" -ForegroundColor Green
Write-Host "   ?? Already organized: $alreadyInDocs files" -ForegroundColor Gray
Write-Host "   ? Not found: $notFound files" -ForegroundColor DarkGray
Write-Host "   ?? Kept in root: README.md" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "? Documentation organized successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Structure:" -ForegroundColor Cyan
Write-Host "   ?? ./README.md (main documentation)" -ForegroundColor White
Write-Host "   ?? ./docs/ (all other documentation)" -ForegroundColor White
Write-Host ""

# List files in docs folder
$docsFiles = Get-ChildItem -Path $docsPath -Filter "*.md" | Sort-Object Name
Write-Host "?? Files in docs folder: $($docsFiles.Count)" -ForegroundColor Cyan
Write-Host ""
