param (
    [string]$CommitMessage = "feat: redesign MCP server cards layout, full interactive plugin system with marketplace, REST client and live markdown preview"
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
    "- 🔌 徹底解決 MCP Server 開關文字裁切問題：將側邊欄寬度適配至 340px，並優化 ToggleSwitch 緊湊佈局與狀態膠囊標籤，名稱與狀態文字 100% 完整清晰呈現。",
    "- ⚡ MCP 伺服器中心全面互動升級：新增「測試連線 (Ping)」模擬延遲檢測、「檢視工具」彈出式完整工具參數檢視器與「刪除」自訂伺服器功能。",
    "- 🧩 全功能實作外掛模組擴充體系 (Plugin Hub)：告別純圖示展示，每個已安裝外掛皆配備一鍵執行的核心功能按鈕：",
    "  * ⚡ Prettier：直接一鍵排版美化當前檔案（支援 JSON/JS/TS/HTML/CSS 等多語言）。",
    "  * 👁️ GitLens：切換編輯器行尾提交者（Author）與相對時間幽靈標註，即時掌握行級版本歷史。",
    "  * 🌈 Rainbow Brackets：動態切換 Monaco 核心彩虹多重漸層配對括號高亮與對齊引導線。",
    "  * 📑 Markdown Preview：一鍵開啟左右雙向即時渲染預覽分欄，支援 GitHub GFM 樣式與代碼高亮。",
    "  * 🤖 Aether Copilot：即時調用 AI 助理進行深層代碼解析、測試生成與重構建議。",
    "  * 🌐 REST Client：一鍵切換底部 API 測試工作台，即時發送 HTTP 請求與格式化 JSON 回應。",
    "- 🛒 外掛市集 (Marketplace) 與一鍵安裝：新增市集分頁，收錄 Python Black、ESLint、Docker Tools、Color Highlight、TODO Highlight 等熱門套件，支援一鍵安裝與自訂本機外掛載入。",
    "- 🌐 底部控制台新增 REST Client API 測試套件：支援 GET/POST/PUT/DELETE/PATCH 方法、自訂 Payload 與即時回應狀態碼、耗時、格式化 JSON 檢視。",
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

