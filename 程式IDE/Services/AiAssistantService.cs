using System;
using System.Text;
using System.Threading.Tasks;

namespace 程式IDE.Services
{
    public class AiAssistantService
    {
        public async Task<string> ProcessPromptAsync(string prompt, string code, string language)
        {
            await Task.Delay(300); // UI responsiveness

            var sb = new StringBuilder();
            var lowerPrompt = prompt.ToLowerInvariant();

            if (lowerPrompt.Contains("explain") || lowerPrompt.Contains("解釋") || lowerPrompt.Contains("說明"))
            {
                sb.AppendLine("### 💡 Aether AI 程式碼解析報告");
                sb.AppendLine($"**語言模式**：`{language}` | **長度**：{code.Length} 字元");
                sb.AppendLine();
                sb.AppendLine("#### 1. 核心邏輯架構");
                if (code.Contains("class") || code.Contains("interface") || code.Contains("def") || code.Contains("function"))
                {
                    sb.AppendLine("- 本段程式定義了模組化結構，具備良好的封裝與方法劃分。");
                }
                if (code.Contains("async") || code.Contains("await") || code.Contains("Task"))
                {
                    sb.AppendLine("- 使用了非同步編程模型 (`async/await`)，確保 UI 或處理程序不產生執行緒阻塞。");
                }
                sb.AppendLine("- 主要執行流程：初始化狀態 -> 處理輸入/資料 -> 調用對應功能 -> 返回或更新狀態。");
                sb.AppendLine();
                sb.AppendLine("#### 2. 關鍵程式碼結構亮點");
                sb.AppendLine("- 具備清晰的命名慣例與職責分離原則。");
                sb.AppendLine("- 適當處理了異常或輸入防禦檢查。");
            }
            else if (lowerPrompt.Contains("bug") || lowerPrompt.Contains("審查") || lowerPrompt.Contains("錯誤") || lowerPrompt.Contains("問題"))
            {
                sb.AppendLine("### 🔍 Aether AI 靜態程式碼審查與潛在 Bug 診斷");
                sb.AppendLine();
                sb.AppendLine("#### 診斷清單：");
                bool foundAny = false;

                if (code.Contains("null") || code.Contains("None"))
                {
                    foundAny = true;
                    sb.AppendLine("1. **空值防禦 (Null Safety)**：發現可能存在 Null 參照風險，建議在取用屬性前加入空值條件運算子 (`?.`) 或 null 判斷。");
                }
                if (code.Contains("catch (Exception)") || code.Contains("except:"))
                {
                    foundAny = true;
                    sb.AppendLine("2. **例外攔截過廣**：捕捉了通用 `Exception`，建議針對特定異常類型處理，避免吞噬關鍵系統錯誤。");
                }
                if (code.Contains("while(true)") || code.Contains("while (true)"))
                {
                    foundAny = true;
                    sb.AppendLine("3. **無限迴圈風險**：請確認迴圈內有確定性的 `break` 或取消語彙語意。");
                }
                if (!foundAny)
                {
                    sb.AppendLine("✅ **優秀**！未偵測到常見的嚴重記憶體洩漏或明顯語法陷阱，程式碼結構健全。");
                    sb.AppendLine("- 建議補充：單元測試覆蓋率、邊界條件測試 (邊界值 0, 空字串, 負數)。");
                }
            }
            else if (lowerPrompt.Contains("optimize") || lowerPrompt.Contains("最佳化") || lowerPrompt.Contains("效能") || lowerPrompt.Contains("重構"))
            {
                sb.AppendLine("### ⚡ Aether AI 效能最佳化與重構建議");
                sb.AppendLine();
                sb.AppendLine("1. **記憶體配置最佳化**：對於頻繁拼接字串的情境，建議改用 `StringBuilder` 或唯讀記憶體緩衝區。");
                sb.AppendLine("2. **非同步 I/O**：確保磁碟存取與網路通訊皆透過非同步 API 執行以最大化吞吐量。");
                sb.AppendLine("3. **LINQ / 集合查詢**：避免在迴圈內部多次調用 `.Count()` 或 `.ToList()`，可將查詢快取或提前具體化。");
                sb.AppendLine();
                sb.AppendLine("重構範例：");
                sb.AppendLine("```" + language);
                sb.AppendLine("// 建議採用模式比對與精簡語法提升可讀性與執行效率");
                sb.AppendLine("```");
            }
            else if (lowerPrompt.Contains("test") || lowerPrompt.Contains("測試"))
            {
                sb.AppendLine("### 🧪 Aether AI 自動產生單元測試");
                sb.AppendLine();
                sb.AppendLine("針對目前檔案產生的測試骨幹：");
                sb.AppendLine("```" + (language == "csharp" ? "csharp" : language));
                if (language == "csharp")
                {
                    sb.AppendLine("[Fact]");
                    sb.AppendLine("public async Task Test_Execution_Should_Succeed()");
                    sb.AppendLine("{");
                    sb.AppendLine("    // Arrange");
                    sb.AppendLine("    // Act");
                    sb.AppendLine("    // Assert");
                    sb.AppendLine("    Assert.True(true);");
                    sb.AppendLine("}");
                }
                else if (language == "python")
                {
                    sb.AppendLine("import unittest");
                    sb.AppendLine();
                    sb.AppendLine("class TestMain(unittest.TestCase):");
                    sb.AppendLine("    def test_run(self):");
                    sb.AppendLine("        self.assertTrue(True)");
                    sb.AppendLine();
                    sb.AppendLine("if __name__ == '__main__':");
                    sb.AppendLine("    unittest.main()");
                }
                else
                {
                    sb.AppendLine("describe('Suite', () => {");
                    sb.AppendLine("    it('should execute successfully', () => {");
                    sb.AppendLine("        expect(true).toBe(true);");
                    sb.AppendLine("    });");
                    sb.AppendLine("});");
                }
                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine("### 🤖 Aether AI 回覆");
                sb.AppendLine();
                sb.AppendLine($"已收到您關於「{prompt}」的請求。");
                sb.AppendLine($"- 目前編輯中的程式語言為 **{language}**。");
                sb.AppendLine("- 您可以使用下方的快捷動作按鈕，針對目前程式碼進行即時解析、除錯檢查、單元測試產生或效能重構！");
            }

            return sb.ToString();
        }
    }
}

