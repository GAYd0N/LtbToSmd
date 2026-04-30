# LtbToSmd 发布脚本
# 用法: .\build\publish.ps1 [版本号]
# 如果不指定版本号，则从 .csproj 中读取

param(
    [string]$Version
)

$ErrorActionPreference = "Stop"

# 切换到项目根目录
Set-Location (Split-Path $PSScriptRoot -Parent)

if (-not $Version) {
    # 从 .csproj 读取版本号
    $csprojPath = "LtbToSmd/LtbToSmd.csproj"
    $csproj = [xml](Get-Content $csprojPath)
    $Version = $csproj.Project.PropertyGroup.Version
    if (-not $Version) {
        Write-Error "无法从 .csproj 读取 Version 属性，请手动指定版本号。"
        exit 1
    }
    Write-Host "[INFO] 从 .csproj 读取版本号: $Version"
}

$outputDir = "publish/v$Version"

Write-Host "[INFO] 开始发布 v$Version ..."
Write-Host "[INFO] 输出目录: $outputDir"

dotnet publish LtbToSmd/LtbToSmd.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $outputDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "发布失败。"
    exit 1
}

Write-Host "[OK] 发布完成！输出目录: $outputDir"

# 检查 git tag 是否已存在
$tagName = "v$Version"
$existingTag = git tag -l $tagName
if (-not $existingTag) {
    Write-Host "[INFO] 可使用以下命令创建 tag 并推送："
    Write-Host "  git tag $tagName"
    Write-Host "  git push origin $tagName"
}
else {
    Write-Host "[INFO] Tag '$tagName' 已存在。"
}
