@echo off
echo Starting POS System...
cd /d "%~dp0"
start "" "POSSystem.WPF\bin\Debug\net8.0-windows7.0\POSSystem.WPF.exe"
echo Application started!
timeout /t 2 >nul
