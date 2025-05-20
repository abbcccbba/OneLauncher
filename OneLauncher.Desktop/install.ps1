# install.ps1

# 配置
$softwareUrl = "https://github.com/abbcccbba/OneLauncher/releases/download/v0.0.4v1.0.0/OneLauncher.Desktop.exe"
$softwareFile = Join-Path (Get-Location) "OneLauncher.Desktop.exe"
$logFile = "$env:TEMP\OneLauncherInstall.log"

# 检查 winget 是否可用
function Test-Winget {
    return (Get-Command "winget" -ErrorAction SilentlyContinue) -ne $null
}

# 检查管理员权限
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Please run this script as Administrator for installing dependencies!" -ForegroundColor Red
    exit 1
}

# 开始日志
Start-Transcript -Path $logFile -Append

# 显示菜单
Write-Host "=== OneLauncher Installation ==="
Write-Host "1. Download OneLauncher only"
Write-Host "2. Download OneLauncher + Install .NET 9 Desktop Runtime"
Write-Host "3. Download OneLauncher + Install .NET 9 Desktop Runtime + Install Temurin JDK 24"
$choice = Read-Host "Please select an option (1-3)"

# 下载 OneLauncher
function Download-OneLauncher {
    Write-Host "Downloading OneLauncher to $(Get-Location)..."
    try {
        Invoke-WebRequest -Uri $softwareUrl -OutFile $softwareFile -ErrorAction Stop
        Write-Host "OneLauncher downloaded successfully!" -ForegroundColor Green
    } catch {
        Write-Host "Failed to download OneLauncher: $_" -ForegroundColor Red
        exit 1
    }
}

# 安装 .NET 9 Desktop Runtime
function Install-DotNet {
    if (-not (Test-Winget)) {
        Write-Host "winget not found! Please install winget or manually install .NET 9 Desktop Runtime." -ForegroundColor Red
        exit 1
    }
    Write-Host "Installing .NET 9 Desktop Runtime..."
    try {
        winget install -e --id Microsoft.DotNet.DesktopRuntime.9 --silent --accept-source-agreements --accept-package-agreements
        Write-Host ".NET 9 Desktop Runtime installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "Failed to install .NET 9 Desktop Runtime: $_" -ForegroundColor Red
        exit 1
    }
}

# 安装 Temurin JDK 24
function Install-OpenJDK {
    if (-not (Test-Winget)) {
        Write-Host "winget not found! Please install winget or manually install Temurin JDK 24." -ForegroundColor Red
        exit 1
    }
    Write-Host "Installing Temurin JDK 24..."
    try {
        winget install -e --id EclipseAdoptium.Temurin.24.JDK --silent --accept-source-agreements --accept-package-agreements
        Write-Host "Temurin JDK 24 installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "Failed to install Temurin JDK 24: $_" -ForegroundColor Red
        exit 1
    }
}

# 根据用户选择执行
switch ($choice) {
    "1" {
        Download-OneLauncher
    }
    "2" {
        Download-OneLauncher
        Install-DotNet
    }
    "3" {
        Download-OneLauncher
        Install-DotNet
        Install-OpenJDK
    }
    default {
        Write-Host "Invalid choice!" -ForegroundColor Red
        Stop-Transcript
        exit 1
    }
}

Write-Host "Installation completed successfully!" -ForegroundColor Green
Stop-Transcript