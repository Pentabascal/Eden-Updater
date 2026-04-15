param(
    [Parameter(Mandatory = $true)]
    [string]$FolderPath
)

$ErrorActionPreference = 'Stop'

# API de GitHub para la última release
$repo = "Eden-CI/Nightly"
$apiUrl = "https://api.github.com/repos/$repo/releases/latest"
$headers = @{
    "User-Agent" = "PowerShell-Nightly-Downloader"
    "Accept"     = "application/vnd.github+json"
}

Write-Host "Obteniendo última release de $repo..."
$release = Invoke-RestMethod -Uri $apiUrl -Headers $headers

# Buscar asset .zip que contenga msvc y amd64
$asset = $release.assets | Where-Object {
    $_.name -match '(?i)msvc' -and
    $_.name -match '(?i)amd64' -and
    $_.name -match '\.zip$'
} | Select-Object -First 1

if (-not $asset) {
    $names = ($release.assets | ForEach-Object { $_.name }) -join ", "
    throw "No se encontró un asset .zip MSVC amd64. Assets disponibles: $names"
}

# Preparar rutas temporales
$tempRoot = Join-Path $env:TEMP ("nightly_" + [Guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $tempRoot $asset.name
$extractPath = Join-Path $tempRoot "extract"

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
New-Item -ItemType Directory -Path $extractPath -Force | Out-Null

if (-not (Test-Path $FolderPath)) {
    New-Item -ItemType Directory -Path $FolderPath -Force | Out-Null
}

Write-Host "Descargando: $($asset.name)"
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $headers

Write-Host "Descomprimiendo..."
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

Write-Host "Copiando y reemplazando en: $FolderPath"
Get-ChildItem -Path $extractPath -Force | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $FolderPath -Recurse -Force
}

# Limpieza
Remove-Item -Path $tempRoot -Recurse -Force

Write-Host "Proceso completado."