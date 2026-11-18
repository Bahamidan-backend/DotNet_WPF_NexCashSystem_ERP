@echo off
echo ============================================
echo     POS System Auto-Updater
echo ============================================
echo.

set LOGFILE="%~2\update_log.txt"
echo [%date% %time%] Starting forced update... > %LOGFILE%

:: 1. Kill the program
echo Closing program and releasing files...
taskkill /f /im POSSystem.WPF.exe
timeout /t 3 /nobreak

:: 2. Extract and replace files
echo Extracting update file and replacing old system...
powershell -Command "Unblock-File -Path '%~1'; try { Expand-Archive -Path '%~1' -DestinationPath '%~2' -Force -ErrorAction Stop; Add-Content -Path '%~2\update_log.txt' -Value 'Update Extracted Successfully!' } catch { Add-Content -Path '%~2\update_log.txt' -Value ('Fatal Error: ' + $_.Exception.Message); exit 1 }"

:: If extraction fails
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Update failed! Please check update_log.txt
    pause
    exit /b
)

:: 3. Cleanup
echo Cleaning up temporary files...
if exist "%~1" del /f /q "%~1"
for %%I in ("%~1") do (
    if exist "%%~dpI" rmdir /s /q "%%~dpI"
)

echo.
echo ============================================
echo     Update Completed Successfully!
echo ============================================
timeout /t 2 /nobreak

:: Restore updated version.json if exists
if exist "%~2\version.json.new" (
    echo Restoring updated version.json...
    move /Y "%~2\version.json.new" "%~2\version.json"
)

:: 4. Restart program
start "" "%~2\POSSystem.WPF.exe"
exit