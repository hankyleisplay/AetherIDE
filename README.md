# AetherIDE 🚀

> 專為 Windows 11 打造的現代化原生 WinUI 3 程式整合開發環境 (IDE)。

[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%20WinUI%203-0078D7?logo=windows)](https://github.com/hankyleisplay/AetherIDE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%2F%2010.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows App SDK](https://img.shields.io/badge/Windows%20App%20SDK-2.5.1-0078D7)](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

---

## ✨ 核心特色 (Key Features)

### 1. 🎨 現代 Windows 11 原生視覺設計
- **Mica 半透明毛玻璃材質**：深度整合 Windows 11 系統設計語言。
- **自訂整合式標題列 (Custom TitleBar)**：包含專案標題、全域搜尋 / 命令列（`Ctrl + P`）、快速執行按鈕、深淺色主題切換。
- **VS Dark / Light 主題**：一鍵無縫切換，編輯器與 WinUI 介面即時同步更新。

### 2. ⚡ Monaco 程式碼編輯核心 (VS Code 同款引擎)
- **多語言語法高亮**：支援 C#、Python、JavaScript、TypeScript、HTML、CSS、JSON、Markdown、C/C++、SQL、PowerShell 等 50+ 種程式語言。
- **智慧自動補全 (IntelliSense) 與程式碼摺疊**。
- **程式碼縮圖 (Minimap)**、括號色彩對應與多游標選取。
- **一鍵格式化文件 (Format Document)**：快捷鍵 `Shift + Alt + F` 即可自動美化排版程式碼。

### 3. 🤖 Aether AI 智慧編程助理
- **💡 程式碼解析**：一鍵解析目前檔案邏輯架構與模組設計。
- **🔍 尋找潛在 Bug**：靜態程式碼審查，診斷 Null 參照與例外處理風險。
- **⚡ 效能最佳化**：提供現代語言特性重構與非同步調用建議。
- **🧪 自動產生單元測試**：支援 C# (xUnit)、Python (unittest) 與 JS 自動測試骨幹生成。

### 4. 📚 程式碼範本庫 (Snippets Library)
- 內建 C# Console App、Python Script、HTML5 Responsive Template、JavaScript Fetch Async、JSON 設定檔等標準範本，點擊即可一鍵插入游標所在位置。

### 5. 🚀 即時編譯執行引擎與整合終端機
- **按 F5 直接執行**：自動偵測檔案類型並調用本機直譯器/編譯器（Python, Node.js, .NET, PowerShell 等）。
- **即時輸出日誌 (Output)**：非同步擷取 stdout/stderr，自動滾動並顯示結束代碼。
- **互動終端機 (PowerShell)**：內建終端機指令輸入框，隨時執行命令。
- **問題診斷清單 (Problems)**：即時匯總編譯與執行期錯誤警告。

---

## ⌨️ 常用快捷鍵指南

| 快捷鍵 | 動作說明 |
| :--- | :--- |
| **`F5`** | 直接執行目前開啟的檔案 |
| **`Shift + F5`** | 終止目前執行的程式 |
| **`Ctrl + S`** | 儲存檔案 |
| **`Ctrl + Shift + S`** | 另存新檔 |
| **`Shift + Alt + F`** | 格式化目前文件 |
| **`Ctrl + N`** | 建立新檔案 |
| **`Ctrl + O`** | 開啟檔案 |
| **`Ctrl + Shift + O`** | 開啟專案資料夾 |
| **`Ctrl + W`** | 關閉目前分頁 |
| **`Ctrl + P`** | 快速搜尋檔案、符號或呼叫 AI |
| **`Ctrl + Shift + F`** | 全域搜尋 |
| **`Ctrl + \``** | 切換顯示/隱藏終端機與輸出面板 |

---

## 🛠️ 開發與建置 (Build & Run)

### 前置需求
- Windows 10 (1809 以上) 或 Windows 11
- .NET 8.0 SDK 或以上
- Visual Studio 2022 (安裝「.NET 桌面開發」及「Windows 應用程式 SDK C# 範本」)

### 建置命令
```powershell
# 還原並建置 x64 版本
dotnet build "程式IDE\程式IDE.csproj" -p:Platform=x64

# 執行應用程式
& "程式IDE\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\AetherIDE.exe"
```

### 自動化版本發布 (Publish Release)
本專案包含自動化版本發布腳本，會自動比對 GitHub 現有 Releases 版本號，並依序遞增（保證不覆蓋舊版本）：
```powershell
powershell -ExecutionPolicy Bypass -File "scripts\Publish-Release.ps1" -CommitMessage "Release new features"
```

---

## 📄 授權條款 (License)
本專案採用 [MIT License](LICENSE) 授權。
# AetherIDE