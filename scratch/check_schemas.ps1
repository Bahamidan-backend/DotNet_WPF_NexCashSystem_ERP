$dbs = @(
    "C:\Users\AlShaheen\source\repos\PosSystemVersion55\POSSystem.WPF\poss.db",
    "C:\Users\AlShaheen\source\repos\PosSystemVersion55\POSSystem.WPF\bin\Debug\net8.0-windows7.0\poss.db",
    "C:\Users\AlShaheen\Desktop\nexSystem\poss.db"
)
$dll = (Get-ChildItem -Path "C:\Users\AlShaheen\source\repos\PosSystemVersion55\POSSystem.WPF\bin\Debug\net8.0-windows7.0" -Filter "Microsoft.Data.Sqlite.dll" | Select-Object -First 1).FullName
[System.Reflection.Assembly]::LoadFrom($dll) | Out-Null

foreach ($db in $dbs) {
    Write-Host "=== Checking: $db ==="
    if (Test-Path $db) {
        $conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db")
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "PRAGMA table_info(WarehouseFromProducts);"
        $reader = $cmd.ExecuteReader()
        $cols = @()
        while ($reader.Read()) {
            $cols += $reader["name"].ToString()
        }
        Write-Host "Columns: $($cols -join ', ')"
        $reader.Close()
        $conn.Close()
    } else {
        Write-Host "File not found!"
    }
}

