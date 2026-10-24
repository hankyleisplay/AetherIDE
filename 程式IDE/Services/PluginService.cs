using System.Collections.ObjectModel;
using 程式IDE.Models;

namespace 程式IDE.Services
{
    public class PluginService
    {
        public ObservableCollection<PluginItem> InstalledPlugins { get; } = new();

        public PluginService()
        {
            InitializeDefaultPlugins();
        }

        private void InitializeDefaultPlugins()
        {
            InstalledPlugins.Add(new PluginItem
            {
                Id = "prettier-formatter",
                Name = "Prettier 程式碼美化排版器",
                Version = "3.2.0",
                Author = "Prettier Team",
                Description = "支援 JavaScript, TypeScript, HTML, CSS, JSON 的業界權威程式碼格式化外掛。",
                Category = "代碼格式化",
                IsEnabled = true,
                IconGlyph = "\uE8E4",
                IconColor = "#F59E0B"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "git-lens-pro",
                Name = "GitLens 程式碼行級歷史追蹤",
                Version = "2.1.4",
                Author = "GitKraken / Aether",
                Description = "在編輯器每行即時顯示作者資訊、提交時間與分支差異視覺化。",
                Category = "版本控制",
                IsEnabled = true,
                IconGlyph = "\uE8CD",
                IconColor = "#3B82F6"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "rainbow-brackets",
                Name = "Rainbow Brackets 彩虹彩色括號",
                Version = "1.5.0",
                Author = "CoenraadS",
                Description = "為多層巢狀圓括號、方括號與花括號提供動態漸層色彩高亮。",
                Category = "語法高亮",
                IsEnabled = true,
                IconGlyph = "\uE790",
                IconColor = "#EC4899"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "markdown-preview",
                Name = "Markdown 即時渲染預覽器",
                Version = "1.2.0",
                Author = "Aether Community",
                Description = "支援即時左右分欄預覽 GitHub Flavored Markdown、表格與數學公式。",
                Category = "文檔預覽",
                IsEnabled = true,
                IconGlyph = "\uE8A5",
                IconColor = "#10B981"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "aether-copilot-plus",
                Name = "Aether Copilot Plus 智慧聯想",
                Version = "2.0.0",
                Author = "Aether AI Team",
                Description = "深度整合 MCP 工具與大型語言模型，提供行內即時灰色虛擬碼自動補全。",
                Category = "AI 擴充",
                IsEnabled = true,
                IconGlyph = "\uE945",
                IconColor = "#8B5CF6"
            });

            InstalledPlugins.Add(new PluginItem
            {
                Id = "rest-client",
                Name = "REST Client API 測試套件",
                Version = "0.8.5",
                Author = "Huachao Mao",
                Description = "直接在 .http 或 .rest 檔案中編寫並發送 HTTP 請求與檢視回應標頭。",
                Category = "網路除錯",
                IsEnabled = false,
                IconGlyph = "\uE774",
                IconColor = "#6366F1"
            });
        }

        public void TogglePlugin(PluginItem plugin)
        {
            plugin.IsEnabled = !plugin.IsEnabled;
        }

        public void AddPlugin(string name, string author, string description, string category)
        {
            var p = new PluginItem
            {
                Id = name.ToLowerInvariant().Replace(" ", "-"),
                Name = name,
                Author = author,
                Description = description,
                Category = category,
                IsEnabled = true,
                IconGlyph = "\uEA86",
                IconColor = "#0078D7"
            };
            InstalledPlugins.Add(p);
        }
    }
}

