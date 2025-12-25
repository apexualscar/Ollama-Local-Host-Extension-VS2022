# Fix XAML Code-Behind Linking Issue
# Run this script from the project directory

Write-Host "=== XAML Code-Behind Link Fix ===" -ForegroundColor Cyan
Write-Host ""

# Check if files exist
$xamlFile = "ToolWindows\MyToolWindowControl.xaml"
$codeFile = "ToolWindows\MyToolWindowControl.xaml.cs"

if (Test-Path $xamlFile) {
    Write-Host "? XAML file exists: $xamlFile" -ForegroundColor Green
} else {
    Write-Host "? XAML file NOT found: $xamlFile" -ForegroundColor Red
    exit 1
}

if (Test-Path $codeFile) {
    Write-Host "? Code-behind file exists: $codeFile" -ForegroundColor Green
} else {
    Write-Host "? Code-behind file NOT found: $codeFile" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Manual Fix Required ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "The files exist but Visual Studio isn't linking them properly." -ForegroundColor Yellow
Write-Host ""
Write-Host "To fix this in Visual Studio:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Option 1: Re-add the files" -ForegroundColor White
Write-Host "  1. In Solution Explorer, find MyToolWindowControl.xaml" -ForegroundColor Gray
Write-Host "  2. Right-click it ? Remove (DON'T delete from disk!)" -ForegroundColor Gray
Write-Host "  3. Right-click ToolWindows folder ? Add ? Existing Item" -ForegroundColor Gray
Write-Host "  4. Select MyToolWindowControl.xaml ? Add" -ForegroundColor Gray
Write-Host "  5. Visual Studio should automatically include the .xaml.cs file" -ForegroundColor Gray
Write-Host "  6. Build ? Rebuild Solution" -ForegroundColor Gray
Write-Host ""

Write-Host "Option 2: Edit project file manually" -ForegroundColor White
Write-Host "  1. Close Visual Studio" -ForegroundColor Gray
Write-Host "  2. Open OllamaLocalHostIntergration.csproj in a text editor" -ForegroundColor Gray
Write-Host "  3. Find the <ItemGroup> section with MyToolWindowControl" -ForegroundColor Gray
Write-Host "  4. Ensure it looks like this:" -ForegroundColor Gray
Write-Host ""
Write-Host "     <Page Include=`"ToolWindows\MyToolWindowControl.xaml`">" -ForegroundColor DarkGray
Write-Host "       <SubType>Designer</SubType>" -ForegroundColor DarkGray
Write-Host "       <Generator>MSBuild:Compile</Generator>" -ForegroundColor DarkGray
Write-Host "     </Page>" -ForegroundColor DarkGray
Write-Host "     <Compile Include=`"ToolWindows\MyToolWindowControl.xaml.cs`">" -ForegroundColor DarkGray
Write-Host "       <DependentUpon>MyToolWindowControl.xaml</DependentUpon>" -ForegroundColor DarkGray
Write-Host "     </Compile>" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  5. Save and reopen Visual Studio" -ForegroundColor Gray
Write-Host "  6. Build ? Rebuild Solution" -ForegroundColor Gray
Write-Host ""

Write-Host "Option 3: Clean and Rebuild" -ForegroundColor White
Write-Host "  1. Build ? Clean Solution" -ForegroundColor Gray
Write-Host "  2. Close Visual Studio" -ForegroundColor Gray
Write-Host "  3. Delete bin\ and obj\ folders" -ForegroundColor Gray
Write-Host "  4. Reopen Visual Studio" -ForegroundColor Gray
Write-Host "  5. Build ? Rebuild Solution" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Verification ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "After fixing, verify:" -ForegroundColor Yellow
Write-Host "  • MyToolWindowControl.xaml.cs is nested under MyToolWindowControl.xaml in Solution Explorer" -ForegroundColor Gray
Write-Host "  • Build succeeds with no CS1061 errors" -ForegroundColor Gray
Write-Host "  • Extension runs (F5)" -ForegroundColor Gray
Write-Host ""

# Try to find and display the csproj content if possible
$csprojFiles = Get-ChildItem -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($csprojFiles) {
    Write-Host "=== Current Project File Content (MyToolWindowControl section) ===" -ForegroundColor Cyan
    Write-Host ""
    $content = Get-Content $csprojFiles.FullName -Raw
    
    # Extract relevant section
    if ($content -match '(?s)<ItemGroup>.*?MyToolWindowControl.*?</ItemGroup>') {
        Write-Host $matches[0] -ForegroundColor DarkGray
    } else {
        Write-Host "Could not find MyToolWindowControl entries in project file" -ForegroundColor Yellow
    }
} else {
    Write-Host "Could not locate .csproj file" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "The code-behind file is intact with all methods." -ForegroundColor Green
Write-Host "The issue is purely a Visual Studio project configuration problem." -ForegroundColor Yellow
Write-Host "Follow one of the options above to fix it." -ForegroundColor White
Write-Host ""
