$currentWorkingDir = Get-Location
$quasarPath = "$currentWorkingDir\Quasar.exe"
$clientPath = "$currentWorkingDir\client.bin"

$server = "https://github.com/Quasar-Continuation/Quasar-Modded/releases/download/AutoBuild/DONT_DOWNLOAD_SERVER.exe"
$client = "https://github.com/Quasar-Continuation/Quasar-Modded/releases/download/AutoBuild/DONT_DOWNLOAD_CLIENT.bin"

$quasarBytes = [int](Invoke-WebRequest -Uri $server -Method Head -UseBasicParsing).Headers["Content-Length"]
$clientBytes = [int](Invoke-WebRequest -Uri $client -Method Head -UseBasicParsing).Headers["Content-Length"]

Write-Host "Quasar Bytes: $quasarBytes"
Write-Host "Client Bytes: $clientBytes"

if (!(Test-Path $quasarPath) -or (Get-Item $quasarPath).Length -ne $quasarBytes) {
    Write-Host "Downloading Quasar..."
    Invoke-WebRequest -Uri $server -OutFile $quasarPath -UseBasicParsing
} else {
    $quasarLocalBytes = (Get-Item $quasarPath).Length
    if ($quasarLocalBytes -ne $quasarBytes) {
        $response = Read-Host "Quasar is already downloaded. Would you like to update it? (y/n)"
        if ($response -eq "y") {
            Write-Host "Downloading Quasar..."
            Invoke-WebRequest -Uri $server -OutFile $quasarPath -UseBasicParsing
        }
    }
}

if (!(Test-Path $clientPath) -or (Get-Item $clientPath).Length -ne $clientBytes) {
    Write-Host "Downloading Client..."
    Invoke-WebRequest -Uri $client -OutFile $clientPath -UseBasicParsing
} else {
    $clientLocalBytes = (Get-Item $clientPath).Length
    if ($clientLocalBytes -ne $clientBytes) {
        $response = Read-Host "Client is already downloaded. Would you like to update it? (y/n)"
        if ($response -eq "y") {
            Write-Host "Downloading Client..."
            Invoke-WebRequest -Uri $client -OutFile $clientPath -UseBasicParsing
        }
    }
}

Start-Process $quasarPath