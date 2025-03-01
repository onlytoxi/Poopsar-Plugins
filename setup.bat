<# ::
@echo off
setlocal enabledelayedexpansion

cd /d %~dp0

powershell -c "iex ((Get-Content '%~f0') -join [Environment]::Newline); iex 'main %*'"
goto :eof
#>

function main {
$currentWorkingDir = Get-Location
$quasarPath = "$currentWorkingDir\Quasar.exe"
$clientPath = "$currentWorkingDir\client.bin"

$server = "https://github.com/Quasar-Continuation/Quasar-Modded/releases/download/AutoBuild/DONT_DOWNLOAD_SERVER.exe"
$client = "https://github.com/Quasar-Continuation/Quasar-Modded/releases/download/AutoBuild/DONT_DOWNLOAD_CLIENT.bin"

function Get-FileSize($url) {
    try {
        return [int](Invoke-WebRequest -Uri $url -Method Head -UseBasicParsing).Headers["Content-Length"]
    } catch {
        Write-Host "Error retrieving file size for: $url" -ForegroundColor Red
        return $null
    }
}

function Terminate-Process($processName) {
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Terminating $processName..."
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
}

function Download-File($url, $destination, $expectedSize) {
    try {
        $webClient = New-Object System.Net.WebClient
        Write-Host "Downloading: $url -> $destination"

        $tempPath = "$destination.tmp"
        $downloadTimer = [System.Diagnostics.Stopwatch]::StartNew()

        $webClient.DownloadFile($url, $tempPath)

        $downloadTimer.Stop()
        if ((Test-Path $tempPath) -and ((Get-Item $tempPath).Length -eq $expectedSize)) {
            Move-Item -Force $tempPath $destination
            Write-Host "Download complete: $destination ($($expectedSize) bytes) in $([math]::Round($downloadTimer.Elapsed.TotalSeconds, 2))s"
        } else {
            Write-Host "Download failed or file size mismatch for $destination" -ForegroundColor Red
        }
    } catch {
        Write-Host "Error downloading: $url" -ForegroundColor Red
    }
}

$quasarBytes = Get-FileSize $server
$clientBytes = Get-FileSize $client

if ($quasarBytes -and $clientBytes) {
    Write-Host "Quasar Size: $quasarBytes bytes"
    Write-Host "Client Size: $clientBytes bytes"

    Terminate-Process "Quasar"

    if (!(Test-Path $quasarPath) -or ((Get-Item $quasarPath).Length -ne $quasarBytes)) {
        $updateQuasar = $true
        if (Test-Path $quasarPath) {
            $localSize = (Get-Item $quasarPath).Length
            $diff = $quasarBytes - $localSize
            $diffText = if ($diff -gt 0) { "+$diff bytes" } else { "$diff bytes" }
            
            Write-Host "Local Quasar.exe size: $localSize bytes"
            Write-Host "Server Quasar.exe size: $quasarBytes bytes"
            Write-Host "Quasar requires an update ($diffText)"
            
            $response = Read-Host "Update? (y/n)"
            if ($response -eq "y") { Download-File $server $quasarPath $quasarBytes }
        }
        if ($updateQuasar) { Download-File $server $quasarPath $quasarBytes }
    }

    if (!(Test-Path $clientPath) -or ((Get-Item $clientPath).Length -ne $clientBytes)) {
        $updateClient = $true
        if (Test-Path $clientPath) {
            $localSize = (Get-Item $clientPath).Length
            $diff = $clientBytes - $localSize
            $diffText = if ($diff -gt 0) { "+$diff bytes" } else { "$diff bytes" }
            Write-Host "Client requires an update ($diffText)"
            $response = Read-Host "Update? (y/n)"
            if ($response -ne "y") { $updateClient = $false }
        }
        if ($updateClient) { Download-File $client $clientPath $clientBytes }
    }

    if (Test-Path $quasarPath) {
        Write-Host "Starting Quasar..."
        Start-Process $quasarPath
    } else {
        Write-Host "Quasar executable not found after download attempt." -ForegroundColor Red
    }
} else {
    Write-Host "Failed to retrieve file sizes. Check your internet connection or the URLs." -ForegroundColor Red
}

}