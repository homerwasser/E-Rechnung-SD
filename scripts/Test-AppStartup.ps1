param(
    [int]$TimeoutSeconds = 8,
    [string]$AppPath = ''
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($AppPath)) {
    $AppPath = Join-Path $repositoryRoot 'src\ERechnung.App\bin\Release\net8.0-windows\ERechnung.App.exe'
}
elseif (-not [System.IO.Path]::IsPathRooted($AppPath)) {
    $AppPath = Join-Path $repositoryRoot $AppPath
}
$AppPath = [System.IO.Path]::GetFullPath($AppPath)

if (-not (Test-Path $AppPath)) {
    throw "Die Release-Anwendung wurde nicht gefunden: $AppPath"
}

$process = Start-Process -FilePath $AppPath -PassThru

try {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 250
        $process.Refresh()

        if ($process.HasExited) {
            throw "Die Anwendung wurde waehrend des Starttests mit Exitcode $($process.ExitCode) beendet."
        }
    } while ($process.MainWindowHandle -eq 0 -and [DateTime]::UtcNow -lt $deadline)

    if ($process.MainWindowHandle -eq 0) {
        throw "Innerhalb von $TimeoutSeconds Sekunden wurde kein Hauptfenster geoeffnet."
    }

    if ($process.MainWindowTitle -ne 'E-Rechnung SD') {
        throw "Unerwartetes Startfenster '$($process.MainWindowTitle)' statt 'E-Rechnung SD'."
    }

    Write-Output "Start-Smoke-Test erfolgreich: genau der erwartete Prozess oeffnete '$($process.MainWindowTitle)'."
}
finally {
    if (-not $process.HasExited) {
        $null = $process.CloseMainWindow()
        if (-not $process.WaitForExit(3000)) {
            $process.Kill()
            $process.WaitForExit()
        }
    }

    $process.Dispose()
}
