param()
$dbPath = Join-Path $PSScriptRoot "POSSystem.WPF\bin\Debug\net8.0-windows7.0\poss.db"
$sqlFile = Join-Path $PSScriptRoot "SeedData.sql"
$sqlitePath = Join-Path $PSScriptRoot "sqlite3.exe"

if (-not (Test-Path $dbPath)) {
    Write-Host "ERROR: DB not found at $dbPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $sqlitePath)) {
    Write-Host "Downloading sqlite3..." -ForegroundColor Cyan
    $zip = Join-Path $PSScriptRoot "sqlite-tools.zip"
    Invoke-WebRequest "https://www.sqlite.org/2024/sqlite-tools-win-x64-3460000.zip" -OutFile $zip -UseBasicParsing
    Expand-Archive $zip -DestinationPath (Join-Path $PSScriptRoot "sqlite-tools") -Force
    $exe = Get-ChildItem (Join-Path $PSScriptRoot "sqlite-tools") -Recurse -Filter "sqlite3.exe" | Select-Object -First 1
    Copy-Item $exe.FullName $sqlitePath
    Remove-Item $zip -Force
    Remove-Item (Join-Path $PSScriptRoot "sqlite-tools") -Recurse -Force
    Write-Host "sqlite3 downloaded." -ForegroundColor Green
}

Write-Host "Running seed data..." -ForegroundColor Cyan
$result = & $sqlitePath $dbPath ".read `"$sqlFile`""
$result | ForEach-Object { Write-Host $_ }
Write-Host "Done! Restart the app to see the new products." -ForegroundColor Green
