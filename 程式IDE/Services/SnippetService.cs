using System.Collections.Generic;

namespace 程式IDE.Services
{
    public class SnippetItem
    {
        public string Title { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public static class SnippetService
    {
        public static List<SnippetItem> GetSnippets()
        {
            return new List<SnippetItem>
            {
                new SnippetItem
                {
                    Title = "C# 主控台應用程式",
                    Language = "csharp",
                    Description = "完整的 C# Program 進入點與命名空間",
                    Code = @"using System;
using System.Threading.Tasks;

namespace AetherApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(""歡迎使用 AetherIDE！"");
            await Task.Delay(500);
            Console.WriteLine(""執行完畢！"");
        }
    }
}
"
                },
                new SnippetItem
                {
                    Title = "Python 腳本範本",
                    Language = "python",
                    Description = "標準 Python __main__ 進入點與函式",
                    Code = @"import sys
import time

def main():
    print(""Hello from AetherIDE Python Environment!"")
    print(f""Python 版本: {sys.version}"")

if __name__ == ""__main__"":
    main()
"
                },
                new SnippetItem
                {
                    Title = "HTML5 現代響應式頁面",
                    Language = "html",
                    Description = "包含現代 CSS 重設與美觀版面的 HTML5 範本",
                    Code = @"<!DOCTYPE html>
<html lang=""zh-TW"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>AetherIDE App</title>
  <style>
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 2rem; background: #0f172a; color: #f8fafc; }
    .card { background: #1e293b; padding: 1.5rem; border-radius: 12px; max-width: 600px; margin: 0 auto; box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1); }
  </style>
</head>
<body>
  <div class=""card"">
    <h1>🚀 AetherIDE Web App</h1>
    <p>現代化、流暢且具備 Mica 質感的開發環境。</p>
  </div>
</body>
</html>
"
                },
                new SnippetItem
                {
                    Title = "JavaScript Async API 請求",
                    Language = "javascript",
                    Description = "使用 fetch 與 async/await 處理 JSON 資料",
                    Code = @"async function fetchData(url) {
  try {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    const data = await response.json();
    console.log(""取得資料成功:"", data);
    return data;
  } catch (error) {
    console.error(""請求失敗:"", error);
  }
}
"
                },
                new SnippetItem
                {
                    Title = "JSON 設定檔範本",
                    Language = "json",
                    Description = "標準應用程式組態設定檔",
                    Code = @"{
  ""name"": ""AetherIDE-Project"",
  ""version"": ""1.0.0"",
  ""environment"": ""Development"",
  ""settings"": {
    ""theme"": ""vs-dark"",
    ""fontSize"": 14,
    ""minimap"": true,
    ""autoSave"": true
  }
}
"
                }
            };
        }
    }
}

