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
function Test-Admin {
    return ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 开始日志
Start-Transcript -Path $logFile -Append

# 显示菜单
Write-Host "=== OneLauncher 安装程序 ==="
Write-Host "1. 仅下载 OneLauncher"
Write-Host "2. 下载 OneLauncher + 安装 .NET 9 Desktop Runtime"
Write-Host "3. 下载 OneLauncher + 安装 .NET 9 Desktop Runtime + 安装 Temurin JDK 24"
$choice = Read-Host "请选择一个选项 (1-3)"

# 下载 OneLauncher
function Download-OneLauncher {
    Write-Host "正在下载 OneLauncher 到 $(Get-Location)..."
    try {
        Invoke-WebRequest -Uri $softwareUrl -OutFile $softwareFile -ErrorAction Stop
        Write-Host "OneLauncher 下载成功！" -ForegroundColor Green
    } catch {
        Write-Host "下载 OneLauncher 失败：$_" -ForegroundColor Red
        Stop-Transcript
        exit 1
    }
}

# 安装 .NET 9 Desktop Runtime
function Install-DotNet {
    if (-not (Test-Winget)) {
        Write-Host "未找到 winget！请安装 winget 或手动安装 .NET 9 Desktop Runtime。" -ForegroundColor Red
        Stop-Transcript
        exit 1
    }
    if (-not (Test-Admin)) {
        Write-Host "安装 .NET 9 Desktop Runtime 需要管理员权限，正在请求权限..."
        try {
            $command = "winget install -e --id Microsoft.DotNet.DesktopRuntime.9 --silent --accept-source-agreements --accept-package-agreements"
            Start-Process -FilePath "powershell" -ArgumentList "-Command $command" -Verb RunAs -Wait
        } catch {
            Write-Host "无法提升权限或安装失败：$_" -ForegroundColor Red
            Stop-Transcript
            exit 1
        }
    } else {
        Write-Host "正在安装 .NET 9 Desktop Runtime..."
        try {
            winget install -e --id Microsoft.DotNet.DesktopRuntime.9 --silent --accept-source-agreements --accept-package-agreements
            Write-Host ".NET 9 Desktop Runtime 安装成功！" -ForegroundColor Green
        } catch {
            Write-Host "安装 .NET 9 Desktop Runtime 失败：$_" -ForegroundColor Red
            Stop-Transcript
            exit 1
        }
    }
}

# 安装 Temurin JDK 24
function Install-OpenJDK {
    if (-not (Test-Winget)) {
        Write-Host "未找到 winget！请安装 winget 或手动安装 Temurin JDK 24。" -ForegroundColor Red
        Stop-Transcript
        exit 1
    }
    if (-not (Test-Admin)) {
        Write-Host "安装 Temurin JDK 24 需要管理员权限，正在请求权限..."
        try {
            $command = "winget install -e --id EclipseAdoptium.Temurin.24.JDK --silent --accept-source-agreements --accept-package-agreements"
            Start-Process -FilePath "powershell" -ArgumentList "-Command $command" -Verb RunAs -Wait
        } catch {
            Write-Host "无法提升权限或安装失败：$_" -ForegroundColor Red
            Stop-Transcript
            exit 1
        }
    } else {
        Write-Host "正在安装 Temurin JDK 24..."
        try {
            winget install -e --id EclipseAdoptium.Temurin.24.JDK --silent --accept-source-agreements --accept-package-agreements
            Write-Host "Temurin JDK 24 安装成功！" -ForegroundColor Green
        } catch {
            Write-Host "安装 Temurin JDK 24 失败：$_" -ForegroundColor Red
            Stop-Transcript
            exit 1
        }
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
        Write-Host "选项无效！" -ForegroundColor Red
        Stop-Transcript
        exit 1
    }
}

# 安装完成提示
Write-Host "安装完成！OneLauncher.Desktop.exe 已下载到 $(Get-Location)。" -ForegroundColor Green
$runChoice = Read-Host "是否立即运行 OneLauncher？(Y/N)"
if ($runChoice -eq "Y" -or $runChoice -eq "y") {
    Write-Host "正在启动 OneLauncher..."
    Start-Process -FilePath $softwareFile
}

Stop-Transcript