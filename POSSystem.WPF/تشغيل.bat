@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo تشغيل نقاط البيع...
echo.
dotnet run
echo.
echo انتهى البرنامج. رمز الإرجاع: %ERRORLEVEL%
pause
