using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using 程式IDE.Models;

namespace 程式IDE.Services
{
    public class McpService
    {
        public ObservableCollection<McpServerConfig> Servers { get; } = new();

        public McpService()
        {
            InitializeDefaultServers();
        }

        private void InitializeDefaultServers()
        {
            var fsServer = new McpServerConfig
            {
                Name = "本機檔案系統 MCP (Filesystem)",
                Command = "npx",
                Arguments = "-y @modelcontextprotocol/server-filesystem .",
                Transport = "stdio",
                IsEnabled = true,
                Status = "已連線"
            };
            fsServer.Tools.Add(new McpToolItem
            {
                ServerName = fsServer.Name,
                Name = "read_file",
                Description = "讀取指定路徑檔案的文字內容與元數據",
                ParametersSummary = "path: string"
            });
            fsServer.Tools.Add(new McpToolItem
            {
                ServerName = fsServer.Name,
                Name = "write_file",
                Description = "建立新檔案或覆寫現有檔案內容",
                ParametersSummary = "path: string, content: string"
            });
            fsServer.Tools.Add(new McpToolItem
            {
                ServerName = fsServer.Name,
                Name = "list_directory",
                Description = "列出目錄下所有子檔案與子資料夾清單",
                ParametersSummary = "path: string"
            });

            var fetchServer = new McpServerConfig
            {
                Name = "網路請求與抓取 MCP (Fetch)",
                Command = "npx",
                Arguments = "-y @modelcontextprotocol/server-fetch",
                Transport = "stdio",
                IsEnabled = true,
                Status = "已連線"
            };
            fetchServer.Tools.Add(new McpToolItem
            {
                ServerName = fetchServer.Name,
                Name = "fetch_webpage",
                Description = "獲取網頁 HTML 並轉換為乾淨的 Markdown 格式",
                ParametersSummary = "url: string, raw?: boolean"
            });

            var memoryServer = new McpServerConfig
            {
                Name = "知識圖譜記憶 MCP (Memory Graph)",
                Command = "npx",
                Arguments = "-y @modelcontextprotocol/server-memory",
                Transport = "stdio",
                IsEnabled = false,
                Status = "已停止"
            };
            memoryServer.Tools.Add(new McpToolItem
            {
                ServerName = memoryServer.Name,
                Name = "create_entities",
                Description = "在專案知識圖譜中建立實體概念節點",
                ParametersSummary = "entities: array"
            });

            Servers.Add(fsServer);
            Servers.Add(fetchServer);
            Servers.Add(memoryServer);
        }

        public void AddServer(string name, string command, string args)
        {
            var server = new McpServerConfig
            {
                Name = name,
                Command = command,
                Arguments = args,
                Transport = "stdio",
                IsEnabled = true,
                Status = "已連線"
            };
            server.Tools.Add(new McpToolItem
            {
                ServerName = name,
                Name = "execute_tool",
                Description = "自訂 MCP 伺服器提供的通用工具調用端點",
                ParametersSummary = "params: object"
            });
            Servers.Add(server);
        }

        public void RemoveServer(McpServerConfig server)
        {
            Servers.Remove(server);
        }

        public void ToggleServerStatus(McpServerConfig server)
        {
            server.IsEnabled = !server.IsEnabled;
            server.Status = server.IsEnabled ? "已連線" : "已停止";
        }

        public List<McpToolItem> GetAllActiveTools()
        {
            return Servers.Where(s => s.IsEnabled).SelectMany(s => s.Tools).ToList();
        }
    }
}

