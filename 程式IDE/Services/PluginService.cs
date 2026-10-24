using System;
using System.Collections.ObjectModel;
using 程式IDE.Models;

namespace 程式IDE.Services
{
    public class PluginService
    {
        public ObservableCollection<PluginItem> InstalledPlugins { get; } = new();
        public ObservableCollection<PluginItem> MarketplacePlugins { get; } = new();

        public event Action<PluginItem>? PluginActionRequested;

        public PluginService()
        {
            InitializeDefaultPlugins();
            InitializeMarketplacePlugins();
        }

        private void InitializeDefaultPlugins()
        {
            InstalledPlugins.Add(new PluginItem
            {
                Id = "prettier-formatter",
                Name = "Prettier 程式碼美化排版器",
                Version = "3.2.0",
                Author = "Prettier Team",
                Description = "業界標準代碼格式化引擎，自動規範縮排、字串引號與括號對齊，支援 JS/TS、HTML、CSS、JSON、C#、Python。",
                Category = "代碼格式化",
                IsEnabled = true,
                IsInstalled = true,
                IconGlyph = "\uE8E4",
                IconColor = "#F59E0B",
                ActionTitle = "執行 Prettier 排版",
                ActionGlyph = "\uE8E4",
                ActionTooltip = "對目前編輯中的檔案執行 Prettier 語法排版 (快捷鍵 Shift+Alt+F)",
                Downloads = "2.8M 下載",
                Rating = "★ 4.9",
                SettingsSummary = "縮排: 4 空格 • 單引號: 是 • 分號: 是"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "git-lens-pro",
                Name = "GitLens 程式碼行級歷史追蹤",
                Version = "2.1.4",
                Author = "GitKraken / Aether",
                Description = "在 Monaco 編輯器每一行即時顯示作者資訊、提交時間與註解（Git Blame），隨游標移動即時更新。",
                Category = "版本控制",
                IsEnabled = true,
                IsInstalled = true,
                IconGlyph = "\uE8CD",
                IconColor = "#3B82F6",
                ActionTitle = "切換 GitLens 歷史標註",
                ActionGlyph = "\uE8CD",
                ActionTooltip = "切換編輯器中每行代碼末尾的 Git 提交作者與時間浮動標籤",
                Downloads = "1.9M 下載",
                Rating = "★ 4.9",
                SettingsSummary = "顯示模式: 行尾幽靈文字 • 格式: 作者 + 相對時間"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "rainbow-brackets",
                Name = "Rainbow Brackets 彩虹色彩括號",
                Version = "1.5.0",
                Author = "CoenraadS",
                Description = "為程式碼中的多層巢狀圓括號 ()、方括號 [] 與花括號 {} 提供動態漸層彩虹色彩高亮與對應引導輔助線。",
                Category = "語法高亮",
                IsEnabled = true,
                IsInstalled = true,
                IconGlyph = "\uE790",
                IconColor = "#EC4899",
                ActionTitle = "切換彩虹括號色彩",
                ActionGlyph = "\uE790",
                ActionTooltip = "切換 Monaco 核心的彩虹括號與括號導引對齊線",
                Downloads = "850K 下載",
                Rating = "★ 4.8",
                SettingsSummary = "漸層色彩: 6 色循環 • 配對輔助線: 已啟用"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "markdown-preview",
                Name = "Markdown 即時渲染預覽器",
                Version = "1.2.0",
                Author = "Aether Community",
                Description = "即時雙向解析當前 Markdown 內容，渲染標題、粗體、清單、表格、引用塊與語法高亮代碼塊。",
                Category = "文檔預覽",
                IsEnabled = true,
                IsInstalled = true,
                IconGlyph = "\uE8A5",
                IconColor = "#10B981",
                ActionTitle = "開啟 Markdown 即時預覽",
                ActionGlyph = "\uE8A5",
                ActionTooltip = "在分頁或浮動視窗中開啟目前 Markdown 文件的即時渲染預覽",
                Downloads = "420K 下載",
                Rating = "★ 4.7",
                SettingsSummary = "風格: GitHub GFM • 代碼高亮: 啟用"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "aether-copilot-plus",
                Name = "Aether Copilot Plus 智慧聯想",
                Version = "2.0.0",
                Author = "Aether AI Team",
                Description = "深度整合 MCP 工具與大型語言模型，支援代碼審查、單元測試生成與游標處上下文自動聯想補全。",
                Category = "AI 擴充",
                IsEnabled = true,
                IsInstalled = true,
                IconGlyph = "\uE945",
                IconColor = "#8B5CF6",
                ActionTitle = "呼叫 Aether AI 助理",
                ActionGlyph = "\uE945",
                ActionTooltip = "針對當前檔案啟用智慧 AI 程式碼解析與重構輔助",
                Downloads = "640K 下載",
                Rating = "★ 4.9",
                SettingsSummary = "模型: Aether Omni • MCP 伺服器連動: 啟用"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "rest-client",
                Name = "REST Client API 測試套件",
                Version = "0.8.5",
                Author = "Huachao Mao",
                Description = "在 IDE 底部直接發送 GET、POST、PUT、DELETE 網路請求，檢視 HTTP 狀態碼、標頭與 JSON 格式化回應。",
                Category = "網路除錯",
                IsEnabled = true,
                IsInstalled = true,
                IconGlyph = "\uE774",
                IconColor = "#6366F1",
                ActionTitle = "開啟 REST API 測試面板",
                ActionGlyph = "\uE774",
                ActionTooltip = "切換至底部 REST Client 控制台進行 HTTP API 測試",
                Downloads = "310K 下載",
                Rating = "★ 4.6",
                SettingsSummary = "支援協定: HTTP/1.1, HTTP/2 • 自動排版 JSON"
            });
        }

        private void InitializeMarketplacePlugins()
        {
            MarketplacePlugins.Add(new PluginItem
            {
                Id = "python-black-formatter",
                Name = "Black Python 嚴格格式化工具",
                Version = "24.1.0",
                Author = "Python Software Foundation",
                Description = "無庸置疑的 Python 程式碼排版標準，自動將 Python 程式碼重構為符合 PEP 8 規範。",
                Category = "Python 工具",
                IsEnabled = false,
                IsInstalled = false,
                IconGlyph = "\uE7BE",
                IconColor = "#3B82F6",
                ActionTitle = "安裝外掛",
                ActionGlyph = "\uE896",
                Downloads = "1.5M 下載",
                Rating = "★ 4.9"
            });

            MarketplacePlugins.Add(new PluginItem
            {
                Id = "eslint-linter",
                Name = "ESLint 代碼品質審查器",
                Version = "9.0.0",
                Author = "OpenJS Foundation",
                Description = "即時分析 JavaScript 與 TypeScript 語法錯誤、潛在 Bug 與最佳實踐規範警告。",
                Category = "代碼檢查",
                IsEnabled = false,
                IsInstalled = false,
                IconGlyph = "\uE7BA",
                IconColor = "#8B5CF6",
                ActionTitle = "安裝外掛",
                ActionGlyph = "\uE896",
                Downloads = "3.2M 下載",
                Rating = "★ 4.8"
            });

            MarketplacePlugins.Add(new PluginItem
            {
                Id = "docker-explorer",
                Name = "Docker 容器與映像檔管理",
                Version = "1.3.0",
                Author = "Microsoft Community",
                Description = "在 AetherIDE 側邊欄快速查看運行中的 Docker 容器、日誌輸出與本地 Image 映像管理。",
                Category = "DevOps",
                IsEnabled = false,
                IsInstalled = false,
                IconGlyph = "\uEB55",
                IconColor = "#0284C7",
                ActionTitle = "安裝外掛",
                ActionGlyph = "\uE896",
                Downloads = "980K 下載",
                Rating = "★ 4.7"
            });

            MarketplacePlugins.Add(new PluginItem
            {
                Id = "color-highlight",
                Name = "Color Highlight 顏色即時預覽",
                Version = "2.2.0",
                Author = "Sergii N",
                Description = "在 CSS、HTML、JSON 中自動在 HEX (#FFFFFF) 與 RGB 顏色代碼文字背景渲染實際色彩。",
                Category = "前端工具",
                IsEnabled = false,
                IsInstalled = false,
                IconGlyph = "\uE790",
                IconColor = "#EF4444",
                ActionTitle = "安裝外掛",
                ActionGlyph = "\uE896",
                Downloads = "720K 下載",
                Rating = "★ 4.8"
            });

            MarketplacePlugins.Add(new PluginItem
            {
                Id = "todo-highlight",
                Name = "TODO / FIXME 標籤螢光高亮",
                Version = "1.0.4",
                Author = "Wayou Liu",
                Description = "在代碼註釋中自動高亮 TODO:、FIXME:、NOTE:、BUG: 等標籤，方便快速巡檢工作待辦事項。",
                Category = "生產力",
                IsEnabled = false,
                IsInstalled = false,
                IconGlyph = "\uE7C3",
                IconColor = "#F59E0B",
                ActionTitle = "安裝外掛",
                ActionGlyph = "\uE896",
                Downloads = "540K 下載",
                Rating = "★ 4.6"
            });
        }

        public void InstallPlugin(PluginItem plugin)
        {
            if (MarketplacePlugins.Contains(plugin))
            {
                MarketplacePlugins.Remove(plugin);
            }
            plugin.IsInstalled = true;
            plugin.IsEnabled = true;
            plugin.ActionTitle = $"執行 {plugin.Name.Split(' ')[0]}";
            plugin.ActionGlyph = "\uE768";
            if (!InstalledPlugins.Contains(plugin))
            {
                InstalledPlugins.Add(plugin);
            }
        }

        public void UninstallPlugin(PluginItem plugin)
        {
            if (InstalledPlugins.Contains(plugin))
            {
                InstalledPlugins.Remove(plugin);
            }
            plugin.IsInstalled = false;
            plugin.IsEnabled = false;
            plugin.ActionTitle = "安裝外掛";
            plugin.ActionGlyph = "\uE896";
            if (!MarketplacePlugins.Contains(plugin))
            {
                MarketplacePlugins.Add(plugin);
            }
        }

        public void TogglePlugin(PluginItem plugin)
        {
            plugin.IsEnabled = !plugin.IsEnabled;
        }

        public void TriggerAction(PluginItem plugin)
        {
            PluginActionRequested?.Invoke(plugin);
        }
    }
}

