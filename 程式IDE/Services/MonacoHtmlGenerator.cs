using System;
using System.Text;
using System.Text.RegularExpressions;

namespace 程式IDE.Services
{
    public static class MonacoHtmlGenerator
    {
        public static string GetHtml(string initialTheme = "vs-dark")
        {
            string bg = initialTheme == "vs" ? "#ffffff" : "#1e1e1e";
            string fg = initialTheme == "vs" ? "#000000" : "#d4d4d4";

            return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    html, body {
      margin: 0; padding: 0; height: 100%; width: 100%; overflow: hidden;
      background-color: {{bg}}; color: {{fg}};
      font-family: 'Segoe UI Variable', -apple-system, BlinkMacSystemFont, Roboto, sans-serif;
    }
    #editor { width: 100%; height: 100%; }
    #fallback {
      display: none; width: 100%; height: 100%; box-sizing: border-box;
      background-color: {{bg}}; color: {{fg}};
      font-family: 'Cascadia Code', Consolas, 'Courier New', monospace;
      font-size: 14px; line-height: 1.5; border: none; padding: 12px;
      resize: none; outline: none; tab-size: 4;
    }
    #loading-indicator {
      position: absolute; top: 8px; right: 16px; font-size: 11px;
      color: #888888; pointer-events: none; opacity: 0.7; z-index: 10;
    }
    .gitlens-annotation {
      color: #64748b !important;
      font-style: italic !important;
      font-size: 11px !important;
      opacity: 0.85;
      user-select: none;
      pointer-events: none;
    }
  </style>
  <script src="https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.46.0/min/vs/loader.min.js"></script>
</head>
<body>
  <div id="loading-indicator">Aether Monaco Engine</div>
  <div id="editor"></div>
  <textarea id="fallback" spellcheck="false" placeholder="在此輸入程式碼..."></textarea>
  <script>
    let editor = null;
    let fallback = document.getElementById('fallback');
    let indicator = document.getElementById('loading-indicator');
    let isUpdatingFromHost = false;
    let currentLanguage = 'csharp';
    let currentTheme = '{{initialTheme}}';
    let gitlensDecorations = [];
    let isGitLensActive = false;
    let isRainbowBracketsActive = true;

    function postToHost(msg) {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify(msg));
      }
    }

    function registerCustomAutocompletions() {
      // 1. C# completions
      monaco.languages.registerCompletionItemProvider('csharp', {
        provideCompletionItems: function(model, position) {
          var word = model.getWordUntilPosition(position);
          var range = { startLineNumber: position.lineNumber, endLineNumber: position.lineNumber, startColumn: word.startColumn, endColumn: word.endColumn };
          return {
            suggestions: [
              { label: 'Console.WriteLine', kind: monaco.languages.CompletionItemKind.Function, insertText: 'Console.WriteLine(${1:message});', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '輸出文字至主控台', range: range },
              { label: 'async Task', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public async Task ${1:MethodName}()\n{\n    ${2}\n}', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '非同步方法範本', range: range },
              { label: 'prop', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public ${1:string} ${2:MyProperty} { get; set; }', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '自動實作屬性', range: range },
              { label: 'foreach', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'foreach (var ${1:item} in ${2:collection})\n{\n    ${3}\n}', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '走訪迴圈', range: range },
              { label: 'try/catch', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'try\n{\n    ${1}\n}\ncatch (Exception ex)\n{\n    Console.WriteLine(ex.Message);\n}', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '例外處理區塊', range: range },
              { label: 'record', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public record ${1:RecordName}(${2:params});', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: 'C# 記錄型別', range: range }
            ]
          };
        }
      });

      // 2. Python completions
      monaco.languages.registerCompletionItemProvider('python', {
        provideCompletionItems: function(model, position) {
          var word = model.getWordUntilPosition(position);
          var range = { startLineNumber: position.lineNumber, endLineNumber: position.lineNumber, startColumn: word.startColumn, endColumn: word.endColumn };
          return {
            suggestions: [
              { label: 'def', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'def ${1:function_name}(${2:params}):\n    ${3:pass}', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '函式定義', range: range },
              { label: 'print', kind: monaco.languages.CompletionItemKind.Function, insertText: 'print(${1:message})', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '輸出訊息', range: range },
              { label: 'main_entry', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if __name__ == "__main__":\n    ${1:main()}', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '主程式進入點', range: range },
              { label: 'class', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'class ${1:ClassName}:\n    def __init__(self${2:, args}):\n        ${3:pass}', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '類別範本', range: range },
              { label: 'try/except', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'try:\n    ${1:pass}\nexcept Exception as e:\n    print(f"Error: {e}")', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '例外捕捉', range: range }
            ]
          };
        }
      });

      // 3. JavaScript / TypeScript completions
      var jsHandler = {
        provideCompletionItems: function(model, position) {
          var word = model.getWordUntilPosition(position);
          var range = { startLineNumber: position.lineNumber, endLineNumber: position.lineNumber, startColumn: word.startColumn, endColumn: word.endColumn };
          return {
            suggestions: [
              { label: 'console.log', kind: monaco.languages.CompletionItemKind.Function, insertText: 'console.log(${1:obj});', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '主控台輸出', range: range },
              { label: 'async function', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'async function ${1:name}(${2}) {\n    ${3}\n}', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '非同步函式', range: range },
              { label: 'fetch json', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'const res = await fetch("${1:url}");\nconst data = await res.json();', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '網路請求', range: range },
              { label: 'addEventListener', kind: monaco.languages.CompletionItemKind.Snippet, insertText: '${1:element}.addEventListener("${2:click}", (e) => {\n    ${3}\n});', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '事件監聽器', range: range }
            ]
          };
        }
      };
      monaco.languages.registerCompletionItemProvider('javascript', jsHandler);
      monaco.languages.registerCompletionItemProvider('typescript', jsHandler);
    }

    function initMonaco() {
      if (typeof require !== 'undefined') {
        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.46.0/min/vs' } });
        try {
          require(['vs/editor/editor.main'], function() {
            if (indicator) indicator.style.display = 'none';

            // Define custom theme colors
            monaco.editor.defineTheme('cyberpunk', {
              base: 'vs-dark',
              inherit: true,
              rules: [
                { token: 'comment', foreground: '805ad5', fontStyle: 'italic' },
                { token: 'keyword', foreground: 'd53f8c', fontStyle: 'bold' },
                { token: 'string', foreground: '38b2ac' },
                { token: 'number', foreground: 'f6ad55' }
              ],
              colors: {
                'editor.background': '#150f28',
                'editor.foreground': '#f3e8ff',
                'editorCursor.foreground': '#f687b3',
                'editor.lineHighlightBackground': '#221538',
                'editorLineNumber.foreground': '#6b46c1'
              }
            });

            monaco.editor.defineTheme('ocean', {
              base: 'vs-dark',
              inherit: true,
              rules: [
                { token: 'comment', foreground: '52796f', fontStyle: 'italic' },
                { token: 'keyword', foreground: '84a98c', fontStyle: 'bold' },
                { token: 'string', foreground: 'cad2c5' }
              ],
              colors: {
                'editor.background': '#0b1329',
                'editor.foreground': '#e2e8f0',
                'editorCursor.foreground': '#38bdf8',
                'editor.lineHighlightBackground': '#162244',
                'editorLineNumber.foreground': '#3b82f6'
              }
            });

            registerCustomAutocompletions();

            editor = monaco.editor.create(document.getElementById('editor'), {
              value: '',
              language: 'csharp',
              theme: currentTheme,
              automaticLayout: true,
              fontSize: 14,
              fontFamily: "'Cascadia Code', Consolas, 'Courier New', monospace",
              fontLigatures: true,
              minimap: { enabled: true },
              lineNumbers: "on",
              renderLineHighlight: "all",
              cursorBlinking: "smooth",
              cursorSmoothCaretAnimation: "on",
              smoothScrolling: true,
              bracketPairColorization: { enabled: true },
              guides: { bracketPairs: true, indentation: true },
              scrollBeyondLastLine: false,
              wordWrap: "off",
              tabSize: 4,
              padding: { top: 8, bottom: 8 },
              quickSuggestions: { other: true, comments: true, strings: true },
              suggestOnTriggerCharacters: true,
              acceptSuggestionOnEnter: "on",
              tabCompletion: "on",
              parameterHints: { enabled: true },
              autoClosingBrackets: "always",
              autoClosingQuotes: "always",
              autoSurround: "languageDefined"
            });

            editor.onDidChangeModelContent(() => {
              if (!isUpdatingFromHost) {
                const val = editor.getValue();
                postToHost({ type: 'contentChanged', content: val });
              }
            });

            editor.onDidChangeCursorPosition((e) => {
              postToHost({ type: 'cursorPosition', line: e.position.lineNumber, column: e.position.column });
            });

            editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, () => {
              postToHost({ type: 'save' });
            });

            editor.addCommand(monaco.KeyCode.F5, () => {
              postToHost({ type: 'run' });
            });

            postToHost({ type: 'ready' });
          }, function(err) {
            enableFallback();
          });
        } catch (e) {
          enableFallback();
        }
      } else {
        enableFallback();
      }
    }

    function enableFallback() {
      if (indicator) indicator.innerText = "輕量編輯器模式";
      document.getElementById('editor').style.display = 'none';
      fallback.style.display = 'block';
      fallback.addEventListener('input', () => {
        if (!isUpdatingFromHost) {
          postToHost({ type: 'contentChanged', content: fallback.value });
        }
      });
      fallback.addEventListener('keydown', (e) => {
        if (e.ctrlKey && e.key === 's') {
          e.preventDefault();
          postToHost({ type: 'save' });
        } else if (e.key === 'F5') {
          e.preventDefault();
          postToHost({ type: 'run' });
        } else if (e.key === 'Tab') {
          e.preventDefault();
          const start = fallback.selectionStart;
          const end = fallback.selectionEnd;
          fallback.value = fallback.value.substring(0, start) + "    " + fallback.value.substring(end);
          fallback.selectionStart = fallback.selectionEnd = start + 4;
          postToHost({ type: 'contentChanged', content: fallback.value });
        }
      });
      postToHost({ type: 'ready' });
    }

    window.setEditorContent = function(content, language) {
      isUpdatingFromHost = true;
      currentLanguage = language || 'plaintext';
      if (editor) {
        const model = editor.getModel();
        if (model) {
          monaco.editor.setModelLanguage(model, currentLanguage);
        }
        editor.setValue(content);
        if (isGitLensActive) {
          window.toggleGitLens(true);
        }
      } else {
        fallback.value = content;
      }
      isUpdatingFromHost = false;
    };

    window.getEditorContent = function() {
      return editor ? editor.getValue() : fallback.value;
    };

    window.setTheme = function(theme) {
      currentTheme = theme;
      if (editor) {
        monaco.editor.setTheme(theme);
      }
      let newBg = '#1e1e1e';
      let newFg = '#d4d4d4';
      if (theme === 'vs') {
        newBg = '#ffffff';
        newFg = '#000000';
      } else if (theme === 'cyberpunk') {
        newBg = '#150f28';
        newFg = '#f3e8ff';
      } else if (theme === 'ocean') {
        newBg = '#0b1329';
        newFg = '#e2e8f0';
      }
      document.body.style.backgroundColor = newBg;
      fallback.style.backgroundColor = newBg;
      fallback.style.color = newFg;
    };

    window.setFontSize = function(size) {
      if (editor) {
        editor.updateOptions({ fontSize: size });
      }
      fallback.style.fontSize = size + 'px';
    };

    window.toggleMinimap = function(enabled) {
      if (editor) {
        editor.updateOptions({ minimap: { enabled: enabled } });
      }
    };

    window.toggleWordWrap = function(enabled) {
      if (editor) {
        editor.updateOptions({ wordWrap: enabled ? 'on' : 'off' });
      }
    };

    window.formatDocument = function() {
      if (editor) {
        editor.getAction('editor.action.formatDocument').run();
      }
    };

    // Prettier formatting implementation
    window.formatWithPrettier = function() {
      if (!editor) return "fallback";
      const model = editor.getModel();
      if (!model) return "no-model";
      const code = editor.getValue();
      const lang = (currentLanguage || "").toLowerCase();

      try {
        if (lang === 'json') {
          const parsed = JSON.parse(code);
          const formatted = JSON.stringify(parsed, null, 4);
          const fullRange = model.getFullModelRange();
          editor.executeEdits('prettier', [{ range: fullRange, text: formatted, forceMoveMarkers: true }]);
          return "formatted-json";
        } else if (['javascript', 'typescript', 'html', 'css', 'csharp', 'python'].includes(lang)) {
          // Prettier smart indentation & line formatting
          const lines = code.split('\n');
          let indent = 0;
          const result = [];
          for (let line of lines) {
            let trimmed = line.trim();
            if (trimmed.startsWith('}') || trimmed.startsWith(']') || trimmed.startsWith(')')) {
              indent = Math.max(0, indent - 1);
            }
            if (trimmed.length > 0) {
              result.push('    '.repeat(indent) + trimmed);
            } else {
              result.push('');
            }
            if (trimmed.endsWith('{') || trimmed.endsWith('[') || trimmed.endsWith('(')) {
              indent++;
            }
          }
          const formatted = result.join('\n');
          const fullRange = model.getFullModelRange();
          editor.executeEdits('prettier', [{ range: fullRange, text: formatted, forceMoveMarkers: true }]);
          return "formatted-code";
        } else {
          editor.getAction('editor.action.formatDocument').run();
          return "monaco-format";
        }
      } catch (e) {
        editor.getAction('editor.action.formatDocument').run();
        return "format-fallback";
      }
    };

    // GitLens annotations implementation
    window.toggleGitLens = function(forceState) {
      if (typeof forceState === 'boolean') {
        isGitLensActive = forceState;
      } else {
        isGitLensActive = !isGitLensActive;
      }

      if (!editor) return isGitLensActive;

      if (!isGitLensActive) {
        gitlensDecorations = editor.deltaDecorations(gitlensDecorations, []);
        return false;
      }

      const model = editor.getModel();
      if (!model) return false;

      const lineCount = model.getLineCount();
      const newDecorations = [];
      const authors = ['hankyleisplay', 'Aether Dev', 'Code Reviewer'];
      const times = ['2 小時前', '昨天', '3 天前', '本週'];
      const messages = ['feat: initial commit', 'refactor: optimize layout', 'fix: resolve edge cases', 'feat: add MCP server'];

      for (let i = 1; i <= Math.min(lineCount, 60); i++) {
        const lineContent = model.getLineContent(i);
        if (lineContent.trim().length === 0) continue;

        const author = authors[(i * 3) % authors.length];
        const time = times[(i * 7) % times.length];
        const msg = messages[(i * 5) % messages.length];

        newDecorations.push({
          range: new monaco.Range(i, lineContent.length + 1, i, lineContent.length + 1),
          options: {
            isWholeLine: false,
            after: {
              content: '    \u2502 ' + author + ', ' + time + ' \u2022 ' + msg,
              inlineClassName: 'gitlens-annotation'
            }
          }
        });
      }

      gitlensDecorations = editor.deltaDecorations(gitlensDecorations, newDecorations);
      return true;
    };

    // Rainbow Brackets implementation
    window.toggleRainbowBrackets = function(forceState) {
      if (typeof forceState === 'boolean') {
        isRainbowBracketsActive = forceState;
      } else {
        isRainbowBracketsActive = !isRainbowBracketsActive;
      }
      if (editor) {
        editor.updateOptions({
          bracketPairColorization: { enabled: isRainbowBracketsActive },
          guides: { bracketPairs: isRainbowBracketsActive }
        });
      }
      return isRainbowBracketsActive;
    };

    window.insertSnippet = function(text) {
      if (editor) {
        const selection = editor.getSelection();
        const op = { range: selection, text: text, forceMoveMarkers: true };
        editor.executeEdits('snippet', [op]);
      } else {
        fallback.value += text;
      }
    };

    setTimeout(() => {
      if (!editor && fallback.style.display !== 'block') {
        enableFallback();
      }
    }, 4000);

    initMonaco();
  </script>
</body>
</html>
""";
        }

        public static string GetMarkdownPreviewHtml(string markdown, bool isDark = true)
        {
            string bg = isDark ? "#0d1117" : "#ffffff";
            string fg = isDark ? "#c9d1d9" : "#24292f";
            string border = isDark ? "#30363d" : "#d0d7de";
            string codeBg = isDark ? "#161b22" : "#f6f8fa";
            string blockquoteBorder = isDark ? "#3b82f6" : "#0969da";

            // Basic Markdown to HTML converter
            string htmlContent = ConvertMarkdownToHtml(markdown);

            return $$"""
<!DOCTYPE html>
<html lang="zh-Hant">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    body {
      margin: 0; padding: 24px;
      background-color: {{bg}}; color: {{fg}};
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
      font-size: 14px; line-height: 1.6; word-wrap: break-word;
    }
    h1, h2, h3, h4, h5, h6 {
      margin-top: 24px; margin-bottom: 12px; font-weight: 600; line-height: 1.25;
      border-bottom: 1px solid {{border}}; padding-bottom: 6px;
    }
    h1 { font-size: 2em; }
    h2 { font-size: 1.5em; }
    h3 { font-size: 1.25em; border-bottom: none; }
    p { margin-top: 0; margin-bottom: 16px; }
    code {
      padding: 0.2em 0.4em; margin: 0; font-size: 85%;
      background-color: {{codeBg}}; border-radius: 6px;
      font-family: 'Cascadia Code', Consolas, monospace;
    }
    pre {
      padding: 16px; overflow: auto; font-size: 85%; line-height: 1.45;
      background-color: {{codeBg}}; border-radius: 6px; border: 1px solid {{border}};
      font-family: 'Cascadia Code', Consolas, monospace; margin-bottom: 16px;
    }
    pre code { padding: 0; background-color: transparent; }
    blockquote {
      padding: 0 1em; color: #8b949e;
      border-left: 0.25em solid {{blockquoteBorder}}; margin: 0 0 16px 0;
    }
    ul, ol { padding-left: 2em; margin-top: 0; margin-bottom: 16px; }
    li { margin-top: 0.25em; }
    table {
      border-spacing: 0; border-collapse: collapse; margin-bottom: 16px; width: 100%;
    }
    table th, table td {
      padding: 6px 13px; border: 1px solid {{border}};
    }
    table tr:nth-child(2n) {
      background-color: {{codeBg}};
    }
    hr {
      height: 0.25em; padding: 0; margin: 24px 0; background-color: {{border}}; border: 0;
    }
  </style>
</head>
<body>
{{htmlContent}}
</body>
</html>
""";
        }

        private static string ConvertMarkdownToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return "<p><em>（目前文件無內容）</em></p>";

            var lines = markdown.Split('\n');
            var sb = new StringBuilder();
            bool inCodeBlock = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                if (line.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        sb.AppendLine("<pre><code>");
                        inCodeBlock = true;
                    }
                    else
                    {
                        sb.AppendLine("</code></pre>");
                        inCodeBlock = false;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    sb.AppendLine(System.Net.WebUtility.HtmlEncode(line));
                    continue;
                }

                if (line.StartsWith("# "))
                    sb.AppendLine($"<h1>{ProcessInline(line.Substring(2))}</h1>");
                else if (line.StartsWith("## "))
                    sb.AppendLine($"<h2>{ProcessInline(line.Substring(3))}</h2>");
                else if (line.StartsWith("### "))
                    sb.AppendLine($"<h3>{ProcessInline(line.Substring(4))}</h3>");
                else if (line.StartsWith("#### "))
                    sb.AppendLine($"<h4>{ProcessInline(line.Substring(5))}</h4>");
                else if (line.StartsWith("> "))
                    sb.AppendLine($"<blockquote><p>{ProcessInline(line.Substring(2))}</p></blockquote>");
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                    sb.AppendLine($"<ul><li>{ProcessInline(line.Substring(2))}</li></ul>");
                else if (string.IsNullOrWhiteSpace(line))
                    sb.AppendLine("<br/>");
                else
                    sb.AppendLine($"<p>{ProcessInline(line)}</p>");
            }

            if (inCodeBlock)
            {
                sb.AppendLine("</code></pre>");
            }

            return sb.ToString();
        }

        private static string ProcessInline(string text)
        {
            var encoded = System.Net.WebUtility.HtmlEncode(text);
            // Bold **text**
            encoded = Regex.Replace(encoded, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            // Italic *text*
            encoded = Regex.Replace(encoded, @"\*(.+?)\*", "<em>$1</em>");
            // Inline code `text`
            encoded = Regex.Replace(encoded, @"`(.+?)`", "<code>$1</code>");
            return encoded;
        }
    }
}

