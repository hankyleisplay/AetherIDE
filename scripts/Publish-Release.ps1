param (
    [string]$CommitMessage = "Update AetherIDE with latest features and improvements"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$RepoOwner = "hankyleisplay"
$RepoName = "AetherIDE"
$ProjectDir = "Z:\source\程式IDE"
$CsprojPath = "$ProjectDir\程式IDE\程式IDE.csproj"
$DistDir = "$ProjectDir\dist"
$PublishOutputDir = "$DistDir\AetherIDE"

Set-Location $ProjectDir

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  AetherIDE GitHub Release & Publish Tool" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Load GitHub Token securely (from env or secrets.json)
$Token = $env:GITHUB_TOKEN
$secretsFile = Join-Path $ProjectDir "secrets.json"
if (-not $Token -and (Test-Path $secretsFile)) {
    try {
        $json = Get-Content $secretsFile -Raw | ConvertFrom-Json
        $Token = $json.GITHUB_TOKEN
    } catch {}
}

if (-not $Token) {
    Write-Error "錯誤：找不到 GitHub Token！請設定 GITHUB_TOKEN 環境變數或建立 secrets.json。"
    exit 1
}

$RemoteUrl = "https://${RepoOwner}:${Token}@github.com/${RepoOwner}/${RepoName}.git"

# 1. Query GitHub for latest release to guarantee unique, incremented version number
Write-Host "--> 查詢 GitHub 現有 Releases 版本..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "token $Token"
    "User-Agent" = "AetherIDE-AutoPublisher"
    "Accept" = "application/vnd.github.v3+json"
}

$releases = @()
try {
    $releases = Invoke-RestMethod -Uri "https://api.github.com/repos/$RepoOwner/$RepoName/releases" -Headers $headers -Method Get
} catch {
    Write-Host "無法取得 releases 或尚無 release: $($_.Exception.Message)" -ForegroundColor Gray
}

# Determine next version
$targetVersion = "1.0.0"
if ($releases -and $releases.Count -gt 0) {
    $latestTag = $releases[0].tag_name
    Write-Host "目前最新 Release: $latestTag" -ForegroundColor Green
    if ($latestTag -match '^v?(\d+)\.(\d+)\.(\d+)') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2]
        $patch = [int]$matches[3] + 1
        $targetVersion = "$major.$minor.$patch"
    } else {
        $targetVersion = "1.0.$($releases.Count)"
    }
} else {
    Write-Host "尚未有任何 Release，設定初始版本為 v1.0.0" -ForegroundColor Green
}

$newTag = "v$targetVersion"
Write-Host "--> 本次發布新版本號: $newTag (絕不覆蓋現有版本)" -ForegroundColor Magenta

# 2. Build Release x64 binary
Write-Host "--> 正在編譯 Release x64 二進位檔..." -ForegroundColor Yellow
if (Test-Path $DistDir) {
    Remove-Item $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PublishOutputDir -Force | Out-Null

dotnet publish $CsprojPath -c Release -p:Platform=x64 -o $PublishOutputDir

$zipPath = "$DistDir\AetherIDE-$newTag-win-x64.zip"
Write-Host "--> 正在建立壓縮套件: $zipPath..." -ForegroundColor Yellow
Compress-Archive -Path "$PublishOutputDir\*" -DestinationPath $zipPath -Force

# 3. Git commit & push
Write-Host "--> 準備推送變更至 GitHub 倉庫..." -ForegroundColor Yellow
git remote set-url origin $RemoteUrl
git branch -M main

git add .
$status = git status --porcelain
if ($status) {
    git commit -m "feat(release): $CommitMessage [$newTag]"
} else {
    Write-Host "無額外檔案變更需要 commit。" -ForegroundColor Gray
}

Write-Host "--> 推送至 GitHub main 分支..." -ForegroundColor Yellow
git push origin main

# Reset remote to clean URL
git remote set-url origin "https://github.com/$RepoOwner/$RepoName.git"

# 4. Create GitHub Release
Write-Host "--> 正在建立 GitHub Release: $newTag..." -ForegroundColor Yellow

$releaseLines = @(
    "## AetherIDE $newTag 發行說明",
    "",
    "主要新功能與改進：",
    "- 全新命名：更名為 AetherIDE，打造全功能 WinUI 3 現代程式整合開發環境。",
    "- Aether AI 智慧助理：內建程式碼解析、靜態除錯審查、效能最佳化與自動單元測試產生。",
    "- 程式碼範本庫 (Snippets Library)：內建 C#、Python、HTML5、JavaScript、JSON 開發範本，支援一鍵游標插入。",
    "- 程式碼格式化 (Format Document)：支援快捷鍵 Shift + Alt + F 自動排版代碼。",
    "- Monaco 核心整合：支援多語言語法高亮、智慧補全、Minimap 縮圖與雙向游標狀態追蹤。",
    "- 原生直譯與編譯執行器：支援 F5 直接執行 Python、Node.js、.NET、PowerShell 腳本，具備非同步輸出日誌與互動終端機。",
    "- Windows 11 Mica 外觀：支援 Mica 半透明材質與 VS Dark / Light 主題切換。",
    "",
    "下載說明：",
    "下載解壓縮下方 AetherIDE-$newTag-win-x64.zip 即可直接執行 AetherIDE.exe！"
)
$releaseBody = $releaseLines -join "`r`n"

$releasePayload = @{
    tag_name = $newTag
    target_commitish = "main"
    name = "AetherIDE $newTag"
    body = $releaseBody
    draft = $false
    prerelease = $false
} | ConvertTo-Json

$createReleaseUri = "https://api.github.com/repos/$RepoOwner/$RepoName/releases"
$createdRelease = Invoke-RestMethod -Uri $createReleaseUri -Headers $headers -Method Post -Body $releasePayload -ContentType "application/json; charset=utf-8"

Write-Host "Release 建立成功! Release ID: $($createdRelease.id), 網址: $($createdRelease.html_url)" -ForegroundColor Green

# 5. Upload release asset zip
$uploadUrl = $createdRelease.upload_url -replace '\{\?name,label\}', "?name=AetherIDE-$newTag-win-x64.zip"
Write-Host "--> 正在上傳發行二進位壓縮檔 ($zipPath)..." -ForegroundColor Yellow

$uploadHeaders = @{
    "Authorization" = "token $Token"
    "User-Agent" = "AetherIDE-AutoPublisher"
    "Content-Type" = "application/zip"
}

$fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
$uploadRes = Invoke-RestMethod -Uri $uploadUrl -Headers $uploadHeaders -Method Post -Body $fileBytes

Write-Host "恭喜！版本 $newTag 已成功發布至 GitHub Release 與倉庫！" -ForegroundColor Green
Write-Host "發行連結: $($createdRelease.html_url)" -ForegroundColor Cyan
Write-Host "下載連結: $($uploadRes.browser_download_url)" -ForegroundColor Cyan
