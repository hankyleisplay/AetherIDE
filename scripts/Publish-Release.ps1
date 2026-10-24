param (
    [string]$CommitMessage = "feat: add MCP server center, plugin extension system, rich autocompletion, and fix light mode theme sync"
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
    "- 🔌 MCP 伺服器中心 (Model Context Protocol)：內建檔案系統、網絡擷取與記憶體圖譜 MCP 連線，支援一鍵新增自訂命令與參數，擴展 AI 工具調用能力。",
    "- 🧩 外掛模組擴充體系 (Plugin Management)：支援模組開關控制，預裝程式碼排版 (Prettier)、GitLens、彩虹括號、Markdown 即時預覽、Aether Copilot Plus 與 REST Client。",
    "- 💡 豐富程式碼自動補全 (IntelliSense)：Monaco 核心整合 C#、Python、JavaScript 智慧提示與關鍵字/方法片段即時補全。",
    "- 🎨 淺色模式 UI 體驗優化：將執行按鈕調整為高對比度翠綠色 (#15803D)，大幅提升辨識度；徹底解決由調色盤或切換主題後，偏好設定 RadioButtons 未同步導致切換分頁又變回深色的問題。",
    "- 🚀 快捷調色盤選單：頂部標題列調色盤支援深色 (VS Dark)、淺色 (VS Light)、賽博龐克紫 (Cyberpunk) 與深海藍 (Ocean Blue)。",
    "- 🛡️ 行尾結束符號標準化：配置 .gitattributes 並全面標準化 CRLF，徹底消除 Visual Studio 開啟時的不一致行尾對話框提示。",
    "",
    "下載說明：",
    "下載解壓縮下方 AetherIDE-$newTag-win-x64.zip 即可免安裝直接啟動 AetherIDE.exe！"
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

