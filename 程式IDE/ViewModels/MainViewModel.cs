using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using 程式IDE.Models;
using 程式IDE.Services;

namespace 程式IDE.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly FileService _fileService = new();
        private readonly ExecutionService _executionService = new();
        private readonly AiAssistantService _aiAssistantService = new();
        public McpService McpService { get; } = new();
        public PluginService PluginService { get; } = new();
        private DispatcherQueue? _dispatcherQueue;

        public MainViewModel()
        {
            _executionService.ProcessStarted += OnProcessStarted;
            _executionService.OutputReceived += OnOutputReceived;
            _executionService.ErrorReceived += OnErrorReceived;
            _executionService.ProcessExited += OnProcessExited;
        }

        public void InitializeDispatcher(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }

        #region Workspace & Files

        private string? _workspacePath;
        public string? WorkspacePath
        {
            get => _workspacePath;
            set => SetProperty(ref _workspacePath, value);
        }

        private string _workspaceName = "尚未開啟資料夾";
        public string WorkspaceName
        {
            get => _workspaceName;
            set => SetProperty(ref _workspaceName, value);
        }

        public ObservableCollection<FileItem> WorkspaceFiles { get; } = new();

        private FileItem? _selectedFileItem;
        public FileItem? SelectedFileItem
        {
            get => _selectedFileItem;
            set => SetProperty(ref _selectedFileItem, value);
        }

        public void LoadWorkspace(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            WorkspacePath = folderPath;
            WorkspaceName = Path.GetFileName(folderPath);

            WorkspaceFiles.Clear();
            var items = _fileService.LoadWorkspace(folderPath);
            foreach (var item in items)
            {
                WorkspaceFiles.Add(item);
            }

            StatusMessage = $"已開啟專案: {WorkspaceName}";
        }

        #endregion

        #region Tabs & Editor

        public ObservableCollection<EditorTabItem> OpenTabs { get; } = new();

        private EditorTabItem? _selectedTab;
        public EditorTabItem? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    OnPropertyChanged(nameof(HasOpenTabs));
                }
            }
        }

        private int _selectedTabIndex = -1;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public bool HasOpenTabs => OpenTabs.Count > 0;

        public event Action<EditorTabItem>? RequestSyncContentToEditor;
        public event Func<Task<string>>? RequestContentFromEditor;
        public event Action<EditorTabItem>? RequestSaveAs;
        public event Action<string>? RequestInsertCodeToEditor;

        public void OpenFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var existing = OpenTabs.FirstOrDefault(t => string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                SelectedTab = existing;
                SelectedTabIndex = OpenTabs.IndexOf(existing);
                return;
            }

            try
            {
                var content = File.ReadAllText(filePath);
                var tab = EditorTabItem.CreateNew(Path.GetFileName(filePath), content, filePath);
                OpenTabs.Add(tab);
                SelectedTab = tab;
                SelectedTabIndex = OpenTabs.Count - 1;
                OnPropertyChanged(nameof(HasOpenTabs));

                CurrentLanguage = tab.Language;
                StatusMessage = $"已開啟 {tab.Title}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"開啟失敗: {ex.Message}";
            }
        }

        public void NewFile(string? defaultTitle = null, string? defaultContent = null)
        {
            int count = OpenTabs.Count(t => t.Title.StartsWith("未命名")) + 1;
            string title = defaultTitle ?? $"未命名-{count}.cs";
            string content = defaultContent ?? "// 開始編寫程式碼...\n";
            var tab = EditorTabItem.CreateNew(title, content, null);
            tab.IsDirty = true;
            OpenTabs.Add(tab);
            SelectedTab = tab;
            SelectedTabIndex = OpenTabs.Count - 1;
            OnPropertyChanged(nameof(HasOpenTabs));
            CurrentLanguage = tab.Language;
            StatusMessage = $"建立新檔案: {title}";
        }

        public async Task SaveActiveFileAsync()
        {
            if (SelectedTab == null) return;

            if (RequestContentFromEditor != null)
            {
                SelectedTab.Content = await RequestContentFromEditor.Invoke();
            }

            if (string.IsNullOrEmpty(SelectedTab.FilePath))
            {
                RequestSaveAs?.Invoke(SelectedTab);
                return;
            }

            try
            {
                await _fileService.WriteFileAsync(SelectedTab.FilePath, SelectedTab.Content);
                SelectedTab.IsDirty = false;
                StatusMessage = $"已儲存 {SelectedTab.Title}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"儲存失敗: {ex.Message}";
            }
        }

        public void CloseTab(EditorTabItem? tab)
        {
            tab ??= SelectedTab;
            if (tab == null) return;

            int index = OpenTabs.IndexOf(tab);
            OpenTabs.Remove(tab);
            OnPropertyChanged(nameof(HasOpenTabs));

            if (OpenTabs.Count > 0)
            {
                int newIndex = Math.Clamp(index, 0, OpenTabs.Count - 1);
                SelectedTab = OpenTabs[newIndex];
                SelectedTabIndex = newIndex;
            }
            else
            {
                SelectedTab = null;
                SelectedTabIndex = -1;
            }
        }

        #endregion

        #region Aether AI Assistant & Code Snippets

        private string _aiPrompt = string.Empty;
        public string AiPrompt
        {
            get => _aiPrompt;
            set => SetProperty(ref _aiPrompt, value);
        }

        private string _aiResponse = "歡迎使用 Aether AI 助理！\n點擊下方快捷按鈕，或輸入您的問題，AI 將為您即時分析目前程式碼。";
        public string AiResponse
        {
            get => _aiResponse;
            set => SetProperty(ref _aiResponse, value);
        }

        private bool _isAiProcessing;
        public bool IsAiProcessing
        {
            get => _isAiProcessing;
            set => SetProperty(ref _isAiProcessing, value);
        }

        public List<SnippetItem> Snippets { get; } = SnippetService.GetSnippets();

        public async Task AskAiAsync(string? prompt = null)
        {
            prompt ??= AiPrompt;
            if (string.IsNullOrWhiteSpace(prompt)) return;

            IsAiProcessing = true;
            AiResponse = "Aether AI 正在思考分析中...";
            StatusMessage = "Aether AI 正在分析程式碼...";

            string currentCode = SelectedTab?.Content ?? "";
            if (RequestContentFromEditor != null)
            {
                currentCode = await RequestContentFromEditor.Invoke();
            }

            // Include connected MCP tools context
            var activeTools = McpService.GetAllActiveTools();
            string mcpInfo = activeTools.Count > 0
                ? $"\n[已連線 MCP 工具: {string.Join(", ", activeTools.Select(t => t.Name))}]"
                : "";

            string result = await _aiAssistantService.ProcessPromptAsync(prompt + mcpInfo, currentCode, CurrentLanguage);
            AiResponse = result;
            IsAiProcessing = false;
            StatusMessage = "Aether AI 分析完成";
        }

        public void InsertSnippet(SnippetItem snippet)
        {
            RequestInsertCodeToEditor?.Invoke(snippet.Code);
            StatusMessage = $"已插入範本: {snippet.Title}";
        }

        #endregion

        #region MCP Servers & Plugins

        public ObservableCollection<McpServerConfig> McpServers => McpService.Servers;
        public ObservableCollection<PluginItem> InstalledPlugins => PluginService.InstalledPlugins;
        public ObservableCollection<PluginItem> MarketplacePlugins => PluginService.MarketplacePlugins;

        public event Action? RequestPrettierFormat;
        public event Action? RequestToggleGitLens;
        public event Action? RequestToggleRainbowBrackets;
        public event Action? RequestMarkdownPreview;

        private string _pluginSearchQuery = string.Empty;
        public string PluginSearchQuery
        {
            get => _pluginSearchQuery;
            set => SetProperty(ref _pluginSearchQuery, value);
        }

        public void ToggleMcpServer(McpServerConfig server)
        {
            McpService.ToggleServerStatus(server);
            StatusMessage = $"MCP 伺服器 '{server.Name}' {server.Status}";
        }

        public void TogglePlugin(PluginItem plugin)
        {
            PluginService.TogglePlugin(plugin);
            StatusMessage = $"外掛模組 '{plugin.Name}' {plugin.StatusText}";
        }

        public void InstallPlugin(PluginItem plugin)
        {
            PluginService.InstallPlugin(plugin);
            StatusMessage = $"已成功安裝外掛模組: {plugin.Name}";
        }

        public void UninstallPlugin(PluginItem plugin)
        {
            PluginService.UninstallPlugin(plugin);
            StatusMessage = $"已解除安裝外掛模組: {plugin.Name}";
        }

        public void TriggerPluginAction(PluginItem plugin)
        {
            switch (plugin.Id)
            {
                case "prettier-formatter":
                    RequestPrettierFormat?.Invoke();
                    StatusMessage = "[Prettier] 正在執行檔案美化排版...";
                    break;

                case "git-lens-pro":
                    RequestToggleGitLens?.Invoke();
                    StatusMessage = "[GitLens] 已切換行級提交者與時間歷史標註";
                    break;

                case "rainbow-brackets":
                    RequestToggleRainbowBrackets?.Invoke();
                    StatusMessage = "[Rainbow Brackets] 已切換彩虹括號色彩";
                    break;

                case "markdown-preview":
                    RequestMarkdownPreview?.Invoke();
                    StatusMessage = "[Markdown Preview] 已開啟即時渲染預覽視窗";
                    break;

                case "aether-copilot-plus":
                    IsSidebarOpen = true;
                    SelectedSidebarIndex = 2; // AI Assistant
                    AiPrompt = "請針對目前的程式碼提供深入的結構解析、潛在邏輯審查與最佳化建議。";
                    _ = AskAiAsync();
                    StatusMessage = "[Aether Copilot] 已呼叫 AI 智慧助理";
                    break;

                case "rest-client":
                    IsBottomPanelOpen = true;
                    SelectedBottomPanelIndex = 3; // REST Client Tab
                    StatusMessage = "[REST Client] 已切換至 API 測試控制台";
                    break;

                default:
                    StatusMessage = $"已觸發外掛操作: {plugin.Name}";
                    break;
            }
        }

        #endregion

        #region Search

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        private bool _matchCase;
        public bool MatchCase
        {
            get => _matchCase;
            set => SetProperty(ref _matchCase, value);
        }

        private bool _matchWholeWord;
        public bool MatchWholeWord
        {
            get => _matchWholeWord;
            set => SetProperty(ref _matchWholeWord, value);
        }

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        public ObservableCollection<SearchResultItem> SearchResults { get; } = new();

        public async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(WorkspacePath) || string.IsNullOrWhiteSpace(SearchQuery)) return;

            IsSearching = true;
            SearchResults.Clear();
            StatusMessage = "正在搜尋...";

            var results = await _fileService.SearchWorkspaceAsync(WorkspacePath, SearchQuery, MatchCase, MatchWholeWord);

            SearchResults.Clear();
            foreach (var r in results)
            {
                SearchResults.Add(r);
            }

            IsSearching = false;
            StatusMessage = $"搜尋完成，共找到 {SearchResults.Count} 處符合";
        }

        #endregion

        #region Execution & Terminal

        private string _outputLog = "AetherIDE 已啟動。\n點擊上方 [▶ 執行] 或按 F5 即可直接執行目前檔案！\n----------------------------------------\n";
        public string OutputLog
        {
            get => _outputLog;
            set => SetProperty(ref _outputLog, value);
        }

        private string _terminalInput = string.Empty;
        public string TerminalInput
        {
            get => _terminalInput;
            set => SetProperty(ref _terminalInput, value);
        }

        private bool _isProcessRunning;
        public bool IsProcessRunning
        {
            get => _isProcessRunning;
            set => SetProperty(ref _isProcessRunning, value);
        }

        private string _runStatusText = "就緒";
        public string RunStatusText
        {
            get => _runStatusText;
            set => SetProperty(ref _runStatusText, value);
        }

        public async Task RunActiveFileAsync()
        {
            if (SelectedTab == null)
            {
                StatusMessage = "無開啟中的檔案可執行";
                return;
            }

            await SaveActiveFileAsync();

            if (string.IsNullOrEmpty(SelectedTab.FilePath))
            {
                StatusMessage = "請先儲存未命名檔案再執行";
                return;
            }

            IsBottomPanelOpen = true;
            SelectedBottomPanelIndex = 0;

            await _executionService.RunFileAsync(SelectedTab.FilePath, WorkspacePath);
        }

        public void StopExecution()
        {
            _executionService.Stop();
        }

        public void ClearOutput()
        {
            OutputLog = string.Empty;
        }

        public async Task SendTerminalCommandAsync()
        {
            if (string.IsNullOrWhiteSpace(TerminalInput)) return;

            var cmd = TerminalInput;
            TerminalInput = string.Empty;

            IsBottomPanelOpen = true;
            SelectedBottomPanelIndex = 0;

            await _executionService.ExecuteCommandAsync(cmd, WorkspacePath);
        }

        public async Task ExecuteCommandAsync(string cmd, string? workingDir = null)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            IsBottomPanelOpen = true;
            SelectedBottomPanelIndex = 0;
            await _executionService.ExecuteCommandAsync(cmd, workingDir ?? WorkspacePath);
        }

        private void OnProcessStarted(string msg)
        {
            RunOnUI(() =>
            {
                IsProcessRunning = true;
                RunStatusText = "執行中...";
                OutputLog += msg;
                StatusMessage = "程式執行中...";
            });
        }

        private void OnOutputReceived(string data)
        {
            RunOnUI(() =>
            {
                OutputLog += data;
            });
        }

        private void OnErrorReceived(string data)
        {
            RunOnUI(() =>
            {
                OutputLog += data;
                Problems.Add(new ProblemItem
                {
                    Severity = "Error",
                    Message = data.Trim(),
                    FilePath = SelectedTab?.FilePath ?? "",
                    Line = 1
                });
                OnPropertyChanged(nameof(ProblemCount));
            });
        }

        private void OnProcessExited(int code)
        {
            RunOnUI(() =>
            {
                IsProcessRunning = false;
                RunStatusText = code == 0 ? "執行成功 (0)" : $"結束代碼: {code}";
                OutputLog += $"\n[處理程序結束，結束代碼: {code}]\n";
                StatusMessage = code == 0 ? "執行完成" : $"執行發生錯誤 (代碼 {code})";
            });
        }

        #endregion

        #region Problems & Diagnostics

        public ObservableCollection<ProblemItem> Problems { get; } = new();

        public int ProblemCount => Problems.Count;

        public void ClearProblems()
        {
            Problems.Clear();
            OnPropertyChanged(nameof(ProblemCount));
        }

        #endregion

        #region Navigation & Layout

        private int _selectedSidebarIndex = 0; // 0: Explorer, 1: Search, 2: AI, 3: Snippets, 4: MCP, 5: Plugins, 6: Run, 7: Settings
        public int SelectedSidebarIndex
        {
            get => _selectedSidebarIndex;
            set => SetProperty(ref _selectedSidebarIndex, value);
        }

        private bool _isSidebarOpen = true;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set => SetProperty(ref _isSidebarOpen, value);
        }

        private bool _isBottomPanelOpen = true;
        public bool IsBottomPanelOpen
        {
            get => _isBottomPanelOpen;
            set => SetProperty(ref _isBottomPanelOpen, value);
        }

        private int _selectedBottomPanelIndex = 0; // 0: Output, 1: Terminal, 2: Problems
        public int SelectedBottomPanelIndex
        {
            get => _selectedBottomPanelIndex;
            set => SetProperty(ref _selectedBottomPanelIndex, value);
        }

        public void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

        public void ToggleBottomPanel() => IsBottomPanelOpen = !IsBottomPanelOpen;

        #endregion

        #region Settings & Appearance

        private bool _isDarkMode = true;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        private int _fontSize = 14;
        public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        private bool _showMinimap = true;
        public bool ShowMinimap
        {
            get => _showMinimap;
            set => SetProperty(ref _showMinimap, value);
        }

        private bool _wordWrap = false;
        public bool WordWrap
        {
            get => _wordWrap;
            set => SetProperty(ref _wordWrap, value);
        }

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }

        #endregion

        #region Status Bar

        private int _currentLine = 1;
        public int CurrentLine
        {
            get => _currentLine;
            set => SetProperty(ref _currentLine, value);
        }

        private int _currentColumn = 1;
        public int CurrentColumn
        {
            get => _currentColumn;
            set => SetProperty(ref _currentColumn, value);
        }

        private string _currentLanguage = "plaintext";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        private string _currentEncoding = "UTF-8";
        public string CurrentEncoding
        {
            get => _currentEncoding;
            set => SetProperty(ref _currentEncoding, value);
        }

        private string _currentIndentation = "空格: 4";
        public string CurrentIndentation
        {
            get => _currentIndentation;
            set => SetProperty(ref _currentIndentation, value);
        }

        private string _statusMessage = "就緒";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region REST Client (API Debugger)

        private string _restMethod = "GET";
        public string RestMethod
        {
            get => _restMethod;
            set => SetProperty(ref _restMethod, value);
        }

        private string _restUrl = "https://api.github.com/zen";
        public string RestUrl
        {
            get => _restUrl;
            set => SetProperty(ref _restUrl, value);
        }

        private string _restRequestBody = "{\n  \"client\": \"AetherIDE\",\n  \"version\": \"1.0.5\"\n}";
        public string RestRequestBody
        {
            get => _restRequestBody;
            set => SetProperty(ref _restRequestBody, value);
        }

        private string _restResponseStatus = "";
        public string RestResponseStatus
        {
            get => _restResponseStatus;
            set => SetProperty(ref _restResponseStatus, value);
        }

        private string _restResponseTime = "";
        public string RestResponseTime
        {
            get => _restResponseTime;
            set => SetProperty(ref _restResponseTime, value);
        }

        private string _restResponseBody = "";
        public string RestResponseBody
        {
            get => _restResponseBody;
            set => SetProperty(ref _restResponseBody, value);
        }

        private bool _isRestSending;
        public bool IsRestSending
        {
            get => _isRestSending;
            set => SetProperty(ref _isRestSending, value);
        }

        public async Task SendRestRequestAsync()
        {
            if (string.IsNullOrWhiteSpace(RestUrl))
            {
                StatusMessage = "請輸入有效的 API 網址";
                return;
            }

            IsRestSending = true;
            RestResponseStatus = "發送中...";
            RestResponseTime = "";
            RestResponseBody = "";

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "AetherIDE-RestClient/1.0.5");

                var request = new System.Net.Http.HttpRequestMessage
                {
                    Method = new System.Net.Http.HttpMethod(RestMethod.ToUpper()),
                    RequestUri = new Uri(RestUrl.Trim())
                };

                if (RestMethod.ToUpper() != "GET" && !string.IsNullOrWhiteSpace(RestRequestBody))
                {
                    request.Content = new System.Net.Http.StringContent(RestRequestBody, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request);
                stopwatch.Stop();

                RestResponseTime = $"耗時: {stopwatch.ElapsedMilliseconds} ms";
                RestResponseStatus = $"{(int)response.StatusCode} {response.ReasonPhrase}";

                string rawBody = await response.Content.ReadAsStringAsync();

                // Format JSON if possible
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(rawBody);
                    RestResponseBody = System.Text.Json.JsonSerializer.Serialize(doc.RootElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
                catch
                {
                    RestResponseBody = rawBody;
                }

                StatusMessage = $"REST 請求成功完成: {RestResponseStatus} ({RestResponseTime})";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RestResponseTime = $"耗時: {stopwatch.ElapsedMilliseconds} ms";
                RestResponseStatus = "請求失敗 (Error)";
                RestResponseBody = $"錯誤詳細資訊:\n{ex.Message}\n\n{ex.StackTrace}";
                StatusMessage = $"REST 請求失敗: {ex.Message}";
            }
            finally
            {
                IsRestSending = false;
            }
        }

        #endregion

        private void RunOnUI(Action action)
        {
            if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
            {
                _dispatcherQueue.TryEnqueue(() => action());
            }
            else
            {
                action();
            }
        }
    }
}

