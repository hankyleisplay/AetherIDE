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
              renderWhitespace: 'selection'
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

