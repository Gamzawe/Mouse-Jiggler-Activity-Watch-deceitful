# Example Usage Script for LogiOptions Single EXE Build
# This script shows how to use the complete build and release script

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LOGIOPTIONS SINGLE EXE BUILD EXAMPLES" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

Write-Host "`nThe main build script is located at: .\script\Publish-SingleExe.ps1" -ForegroundColor Green

Write-Host "`n=== EXAMPLE COMMANDS ===" -ForegroundColor Yellow

Write-Host "`n1. Full build and publish (recommended for releases):" -ForegroundColor Cyan
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Clean -Build -Publish" -ForegroundColor Gray

Write-Host "`n2. Build only (for testing):" -ForegroundColor Cyan
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Build" -ForegroundColor Gray

Write-Host "`n3. Clean and build (without publishing):" -ForegroundColor Cyan
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Clean -Build" -ForegroundColor Gray

Write-Host "`n4. Publish only (if already built):" -ForegroundColor Cyan
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Publish" -ForegroundColor Gray

Write-Host "`n5. Build, publish, and test:" -ForegroundColor Cyan
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Clean -Build -Publish -Test" -ForegroundColor Gray

Write-Host "`n6. Build, publish, and sign (if certificates available):" -ForegroundColor Cyan
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Clean -Build -Publish -Sign" -ForegroundColor Gray

Write-Host "`n7. Custom configuration:" -ForegroundColor Cyan
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Configuration Release -Runtime win-x64 -Version 2.0.0 -OutputDir `".\MyReleases`" -Clean -Build -Publish" -ForegroundColor Gray

Write-Host "`n=== QUICK START ===" -ForegroundColor Yellow

Write-Host "`nTo create a complete single EXE release, run:" -ForegroundColor Green
Write-Host "   cd `"d:\My_Main_folder\Sofa\Side-Projects\config`"" -ForegroundColor Gray
Write-Host "   powershell -ExecutionPolicy Bypass -File `".\script\Publish-SingleExe.ps1`" -Clean -Build -Publish" -ForegroundColor Gray

Write-Host "`nThis will:" -ForegroundColor Cyan
Write-Host "   1. Clean all build artifacts" -ForegroundColor Gray
Write-Host "   2. Build MacroEngine.Core and encrypt it" -ForegroundColor Gray
Write-Host "   3. Build the main project" -ForegroundColor Gray
Write-Host "   4. Publish as a single self-contained EXE" -ForegroundColor Gray
Write-Host "   5. Create output in .\Releases\ directory" -ForegroundColor Gray

Write-Host "`n=== OUTPUT LOCATION ===" -ForegroundColor Yellow
Write-Host "`nPublished executables will be saved to:" -ForegroundColor Cyan
Write-Host "   .\Releases\LogiOptions-v{version}-{timestamp}-{runtime}\" -ForegroundColor Gray
Write-Host "`nExample:" -ForegroundColor Cyan
Write-Host "   .\Releases\LogiOptions-v1.0.0-20250705-143022-win-x64\LogiOptions.exe" -ForegroundColor Gray

Write-Host "`n=== TROUBLESHOOTING ===" -ForegroundColor Yellow

Write-Host "`nIf build fails:" -ForegroundColor Cyan
Write-Host "   1. Make sure .NET 10.0+ SDK is installed" -ForegroundColor Gray
Write-Host "   2. Run as Administrator if deploying as service" -ForegroundColor Gray
Write-Host "   3. Check for running LogiOptions processes" -ForegroundColor Gray

Write-Host "`nFor more details, see:" -ForegroundColor Cyan
Write-Host "   - .\BUILD_GUIDE.md" -ForegroundColor Gray
Write-Host "   - .\DEPLOYMENT_GUIDE.md" -ForegroundColor Gray

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "Ready to build!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan