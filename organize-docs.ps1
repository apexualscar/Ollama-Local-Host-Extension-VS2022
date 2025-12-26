# Script to organize documentation files into docs folder
# Keeps README.md in root, moves all other .md files to docs/

Write-Host "?? Organizing documentation files..." -ForegroundColor Cyan
Write-Host ""

# Create docs directory if it doesn't exist
$docsPath = "docs"
if (-not (Test-Path $docsPath)) {
    New-Item -Path $docsPath -ItemType Directory | Out-Null
    Write-Host "? Created 'docs' folder" -ForegroundColor Green
} else {
    Write-Host "?? 'docs' folder already exists" -ForegroundColor Yellow
}

# Get all .md files in root directory (not in subdirectories)
$mdFiles = Get-ChildItem -Path "." -Filter "*.md" -File | Where-Object {
    # Exclude README.md (keep in root)
    $_.Name -ne "README.md"
}

Write-Host ""
Write-Host "?? Found $($mdFiles.Count) documentation files to move:" -ForegroundColor Cyan
Write-Host ""

$movedCount = 0
$skippedCount = 0

foreach ($file in $mdFiles) {
    $destPath = Join-Path $docsPath $file.Name
    
    # Check if file already exists in docs
    if (Test-Path $destPath) {
        Write-Host "??  Skipped: $($file.Name) (already exists in docs)" -ForegroundColor Yellow
        $skippedCount++
    } else {
        try {
            Move-Item -Path $file.FullName -Destination $destPath -Force
            Write-Host "? Moved: $($file.Name)" -ForegroundColor Green
            $movedCount++
        } catch {
            Write-Host "? Failed: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?? Summary:" -ForegroundColor Cyan
Write-Host "   ? Moved: $movedCount files" -ForegroundColor Green
Write-Host "   ??  Skipped: $skippedCount files" -ForegroundColor Yellow
Write-Host "   ?? Kept in root: README.md" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "? Documentation organized!" -ForegroundColor Green
Write-Host "   Root: README.md" -ForegroundColor White
Write-Host "   Docs: $docsPath\" -ForegroundColor White
Write-Host ""
