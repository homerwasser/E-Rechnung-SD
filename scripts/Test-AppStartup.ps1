param(
    [int]$TimeoutSeconds = 8
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$appPath = Join-Path $repositoryRoot 'src\ERechnung.App\bin\Release\net8.0-windows\ERechnung.App.exe'

if (-not (Test-Path $appPath)) {
    throw "Die Release-Anwendung wurde nicht gefunden: $appPath"
}

$process = Start-Process -FilePath $appPath -PassThru

try {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 250
        $process.Refresh()

        if ($process.HasExited) {
            throw "Die Anwendung wurde während des Starttests mit Exitcode $($process.ExitCode) beendet."
        }
    } while ($process.MainWindowHandle -eq 0 -and [DateTime]::UtcNow -lt $deadline)

    if ($process.MainWindowHandle -eq 0) {
        throw "Innerhalb von $TimeoutSeconds Sekunden wurde kein Hauptfenster geöffnet."
    }

    if ($process.MainWindowTitle -ne 'E-Rechnung SD') {
        throw "Unerwartetes Startfenster '$($process.MainWindowTitle)' statt 'E-Rechnung SD'."
    }

    Write-Output "Start-Smoke-Test erfolgreich: genau der erwartete Prozess öffnete '$($process.MainWindowTitle)'."
}
finally {
    if (-not $process.HasExited) {
        $null = $process.CloseMainWindow()
        if (-not $process.WaitForExit(3000)) {
            $process.Kill($true)
            $process.WaitForExit()
        }
    }

    $process.Dispose()
}
