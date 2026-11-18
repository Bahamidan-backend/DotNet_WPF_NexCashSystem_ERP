Add-Type -Path "C:\Users\AlShaheen\Desktop\nexSystem\e_sqlite3.dll" -ErrorAction SilentlyContinue
# Check using sqlite3 CLI or python or via dotnet
$csProj = "POSSystem.WPF\POSSystem.WPF.csproj"
$dbPath = "C:\Users\AlShaheen\Desktop\nexSystem\poss.db"
if (Test-Path $dbPath) {
    Write-Output "Found poss.db at $dbPath"
}

