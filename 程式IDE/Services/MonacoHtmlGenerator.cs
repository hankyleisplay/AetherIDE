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
              { label: 'arrow function', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'const ${1:fn} = (${2:args}) => {\n    ${3}\n};', insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet, detail: '箭頭函式', range: range }
            ]
          };
        }
      };
      monaco.languages.registerCompletionItemProvider('javascript', jsHandler);
      monaco.languages.registerCompletionItemProvider('typescript', jsHandler);
    }

    function initMonaco() {
      if (typeof require !== 'undefined') {
        try {
          require.config({ paths: { vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.46.0/min/vs' } });
          require(['vs/editor/editor.main'], function () {
            indicator.style.display = 'none';

            // Define custom themes
            monaco.editor.defineTheme('cyberpunk', {
              base: 'vs-dark',
              inherit: true,
              rules: [
                { token: 'comment', foreground: '7c6f9b', fontStyle: 'italic' },
                { token: 'keyword', foreground: 'f43f5e', fontStyle: 'bold' },
                { token: 'string', foreground: '06b6d4' },
                { token: 'number', foreground: 'f59e0b' }
              ],
              colors: {
                'editor.background': '#150f28',
                'editor.foreground': '#f3e8ff',
                'editorCursor.foreground': '#ec4899',
                'editor.lineHighlightBackground': '#221544'
              }
            });

            monaco.editor.defineTheme('ocean', {
              base: 'vs-dark',
              inherit: true,
              rules: [
                { token: 'comment', foreground: '64748b', fontStyle: 'italic' },
                { token: 'keyword', foreground: '38bdf8', fontStyle: 'bold' },
                { token: 'string', foreground: '34d399' },
                { token: 'number', foreground: 'fbbf24' }
              ],
              colors: {
                'editor.background': '#0b1329',
                'editor.foreground': '#e2e8f0',
                'editorCursor.foreground': '#38bdf8',
                'editor.lineHighlightBackground': '#142144'
              }
            });

            // Register rich custom completions
            registerCustomAutocompletions();

            editor = monaco.editor.create(document.getElementById('editor'), {
              value: '',
              language: currentLanguage,
              theme: currentTheme,
              automaticLayout: true,
              fontSize: 14,
              fontFamily: "'Cascadia Code', 'Fira Code', Consolas, 'Courier New', monospace",
              minimap: { enabled: true },
              lineNumbers: 'on',
              roundedSelection: true,
              scrollBeyondLastLine: false,
              cursorBlinking: 'smooth',
              smoothScrolling: true,
              tabSize: 4,
              renderWhitespace: 'selection',
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
    }
}

