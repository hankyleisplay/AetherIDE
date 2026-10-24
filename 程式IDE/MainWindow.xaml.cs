using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Graphics;
using 程式IDE.Models;
using 程式IDE.Services;
using 程式IDE.ViewModels;

namespace 程式IDE
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        private bool _isWebViewReady;
        private string? _pendingContent;
        private string? _pendingLanguage;

        public MainWindow()
        {
            InitializeComponent();

            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = ElementTheme.Dark;
            }

            // Set up ViewModel dispatcher & events
            ViewModel.InitializeDispatcher(this.DispatcherQueue);
            ViewModel.RequestContentFromEditor += GetEditorContentAsync;
            ViewModel.RequestSaveAs += OnRequestSaveAs;
            ViewModel.RequestInsertCodeToEditor += OnRequestInsertCode;

            // Configure Window
            Title = "AetherIDE";
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBarDragRegion);

            // Resize window to comfortable IDE dimensions
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                appWindow.Resize(new SizeInt32(1366, 820));
            }

            // Initialize Monaco WebView2
            InitializeWebViewAsync();

            // Auto-scroll output when new log arrives
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.OutputLog))
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        OutputTextBox.SelectionStart = OutputTextBox.Text.Length;
                        OutputTextBox.SelectionLength = 0;
                    });
                }
            };

            // Auto-load current workspace directory
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                if (Directory.Exists(currentDir))
                {
                    ViewModel.LoadWorkspace(currentDir);
                }
            }
            catch { }

            // Load initial file
            ViewModel.NewFile();
        }

        #region Value Converters / Helpers for x:Bind

        public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BoolToVisibility(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InverseBoolToVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
        public Visibility InverseBoolToVisibility(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;

        #endregion

        #region WebView2 & Monaco Integration

        private async void InitializeWebViewAsync()
        {
            try
            {
                await EditorWebView.EnsureCoreWebView2Async();
                EditorWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                EditorWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                EditorWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                string initialHtml = MonacoHtmlGenerator.GetHtml(ViewModel.IsDarkMode ? "vs-dark" : "vs");
                EditorWebView.NavigateToString(initialHtml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
            }
        }

        private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var json = args.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(json)) return;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) return;

                string type = typeProp.GetString() ?? "";

                switch (type)
                {
                    case "ready":
                        _isWebViewReady = true;
                        if (_pendingContent != null)
                        {
                            UpdateEditorContent(_pendingContent, _pendingLanguage ?? "plaintext");
                            _pendingContent = null;
                            _pendingLanguage = null;
                        }
                        else if (ViewModel.SelectedTab != null)
                        {
                            UpdateEditorContent(ViewModel.SelectedTab.Content, ViewModel.SelectedTab.Language);
                        }
                        break;

                    case "contentChanged":
                        if (ViewModel.SelectedTab != null && root.TryGetProperty("content", out var contentProp))
                        {
                            var newContent = contentProp.GetString() ?? "";
                            if (ViewModel.SelectedTab.Content != newContent)
                            {
                                ViewModel.SelectedTab.Content = newContent;
                                ViewModel.SelectedTab.IsDirty = true;
                            }
                        }
                        break;

                    case "cursorPosition":
                        if (root.TryGetProperty("line", out var lineProp) && root.TryGetProperty("column", out var colProp))
                        {
                            ViewModel.CurrentLine = lineProp.GetInt32();
                            ViewModel.CurrentColumn = colProp.GetInt32();
                        }
                        break;

                    case "save":
                        _ = ViewModel.SaveActiveFileAsync();
                        break;

                    case "run":
                        _ = ViewModel.RunActiveFileAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing Monaco message: {ex.Message}");
            }
        }

        private async void UpdateEditorContent(string content, string language)
        {
            if (!_isWebViewReady || EditorWebView.CoreWebView2 == null)
            {
                _pendingContent = content;
                _pendingLanguage = language;
                return;
            }

            try
            {
                var safeContent = JsonSerializer.Serialize(content);
                var safeLang = JsonSerializer.Serialize(language);
                await EditorWebView.ExecuteScriptAsync($"window.setEditorContent({safeContent}, {safeLang});");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateEditorContent failed: {ex.Message}");
            }
        }

        private async Task<string> GetEditorContentAsync()
        {
            if (!_isWebViewReady || EditorWebView.CoreWebView2 == null)
            {
                return ViewModel.SelectedTab?.Content ?? "";
            }

            try
            {
                string resultJson = await EditorWebView.ExecuteScriptAsync("window.getEditorContent();");
                return JsonSerializer.Deserialize<string>(resultJson) ?? ViewModel.SelectedTab?.Content ?? "";
            }
            catch
            {
                return ViewModel.SelectedTab?.Content ?? "";
            }
        }

        private async void OnRequestInsertCode(string code)
        {
            if (!_isWebViewReady || EditorWebView.CoreWebView2 == null) return;
            var safeCode = JsonSerializer.Serialize(code);
            await EditorWebView.ExecuteScriptAsync($"window.insertSnippet({safeCode});");
        }

        private async void ApplyThemeToEditor(bool isDark)
        {
            if (!_isWebViewReady || EditorWebView.CoreWebView2 == null) return;
            string theme = isDark ? "vs-dark" : "vs";
            await EditorWebView.ExecuteScriptAsync($"window.setTheme('{theme}');");
        }

        private async void ApplyFontSizeToEditor(int size)
        {
            if (!_isWebViewReady || EditorWebView.CoreWebView2 == null) return;
            await EditorWebView.ExecuteScriptAsync($"window.setFontSize({size});");
        }

        private async void ApplyMinimapToEditor(bool enabled)
        {
            if (!_isWebViewReady || EditorWebView.CoreWebView2 == null) return;
            await EditorWebView.ExecuteScriptAsync($"window.toggleMinimap({(enabled ? "true" : "false")});");
        }

        private async void ApplyWordWrapToEditor(bool enabled)
        {
            if (!_isWebViewReady || EditorWebView.CoreWebView2 == null) return;
            await EditorWebView.ExecuteScriptAsync($"window.toggleWordWrap({(enabled ? "true" : "false")});");
        }

        #endregion

        #region Title Bar & Menu Handlers

        private void TitleSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.QueryText)) return;

            string query = args.QueryText.Trim();
            if (query.StartsWith("?"))
            {
                BtnNavAi_Click(this, new RoutedEventArgs());
                ViewModel.AiPrompt = query.Substring(1).Trim();
                _ = ViewModel.AskAiAsync();
            }
            else
            {
                BtnNavSearch_Click(this, new RoutedEventArgs());
                ViewModel.SearchQuery = query;
                _ = ViewModel.ExecuteSearchAsync();
            }
        }

        private async void BtnQuickRun_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.RunActiveFileAsync();
        }

        private void BtnQuickStop_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StopExecution();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveActiveFileAsync();
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleTheme();
            ApplyThemeToEditor(ViewModel.IsDarkMode);

            if (Content is FrameworkElement fe)
            {
                fe.RequestedTheme = ViewModel.IsDarkMode ? ElementTheme.Dark : ElementTheme.Light;
            }
        }

        private void ThemeOptionDark_Click(object sender, RoutedEventArgs e)
        {
            SetAppTheme(true, "vs-dark", "已切換至深色主題 (VS Dark)");
        }

        private void ThemeOptionLight_Click(object sender, RoutedEventArgs e)
        {
            SetAppTheme(false, "vs", "已切換至淺色主題 (VS Light)");
        }

        private void ThemeOptionCyberpunk_Click(object sender, RoutedEventArgs e)
        {
            SetAppTheme(true, "cyberpunk", "已切換至賽博龐克紫主題 (Cyberpunk Dark)");
        }

        private void ThemeOptionOcean_Click(object sender, RoutedEventArgs e)
        {
            SetAppTheme(true, "ocean", "已切換至海洋深藍主題 (Ocean Blue)");
        }

        private async void SetAppTheme(bool isDark, string monacoTheme, string statusText)
        {
            ViewModel.IsDarkMode = isDark;
            if (Content is FrameworkElement fe)
            {
                fe.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
            }
            if (_isWebViewReady && EditorWebView.CoreWebView2 != null)
            {
                await EditorWebView.ExecuteScriptAsync($"window.setTheme('{monacoTheme}');");
            }
            ViewModel.StatusMessage = statusText;
        }

        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleSidebar();
        }

        private void BtnToggleBottomPanel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleBottomPanel();
        }

        private void MenuNewFile_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NewFile();
        }

        private void MenuNewCSharp_Click(object sender, RoutedEventArgs e)
        {
            var snippet = ViewModel.Snippets.Find(s => s.Language == "csharp");
            ViewModel.NewFile("Program.cs", snippet?.Code);
        }

        private void MenuNewPython_Click(object sender, RoutedEventArgs e)
        {
            var snippet = ViewModel.Snippets.Find(s => s.Language == "python");
            ViewModel.NewFile("main.py", snippet?.Code);
        }

        private void MenuNewHtml_Click(object sender, RoutedEventArgs e)
        {
            var snippet = ViewModel.Snippets.Find(s => s.Language == "html");
            ViewModel.NewFile("index.html", snippet?.Code);
        }

        private void MenuNewJson_Click(object sender, RoutedEventArgs e)
        {
            var snippet = ViewModel.Snippets.Find(s => s.Language == "json");
            ViewModel.NewFile("config.json", snippet?.Code);
        }

        private async void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                picker.FileTypeFilter.Add("*");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    ViewModel.OpenFile(file.Path);
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"選取檔案失敗: {ex.Message}";
            }
        }

        private async void MenuOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add("*");

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    ViewModel.LoadWorkspace(folder.Path);
                    BtnNavExplorer_Click(this, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"選取資料夾失敗: {ex.Message}";
            }
        }

        private async void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedTab != null)
            {
                await SaveAsAsync(ViewModel.SelectedTab);
            }
        }

        private async void OnRequestSaveAs(EditorTabItem tab)
        {
            await SaveAsAsync(tab);
        }

        private async Task SaveAsAsync(EditorTabItem tab)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("程式原始碼與檔案", new List<string>() { "." });
                picker.SuggestedFileName = tab.Title.Replace(" •", "");

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    tab.FilePath = file.Path;
                    tab.Title = file.Name;
                    tab.Language = EditorTabItem.DetectLanguage(file.Path);
                    await File.WriteAllTextAsync(file.Path, tab.Content);
                    tab.IsDirty = false;
                    ViewModel.StatusMessage = $"已另存為 {file.Name}";
                    UpdateEditorContent(tab.Content, tab.Language);
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"另存新檔失敗: {ex.Message}";
            }
        }

        private void MenuCloseTab_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseTab(null);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void MenuFormatDocument_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewReady && EditorWebView.CoreWebView2 != null)
            {
                await EditorWebView.ExecuteScriptAsync("window.formatDocument();");
                ViewModel.StatusMessage = "文件已格式化";
            }
        }

        private async void MenuFind_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewReady && EditorWebView.CoreWebView2 != null)
            {
                await EditorWebView.ExecuteScriptAsync("if (editor) { editor.getAction('actions.find').run(); }");
            }
        }

        private void MenuFindInFiles_Click(object sender, RoutedEventArgs e)
        {
            BtnNavSearch_Click(this, new RoutedEventArgs());
        }

        private void MenuZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.FontSize < 28)
            {
                ViewModel.FontSize += 2;
                ApplyFontSizeToEditor(ViewModel.FontSize);
            }
        }

        private void MenuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.FontSize > 10)
            {
                ViewModel.FontSize -= 2;
                ApplyFontSizeToEditor(ViewModel.FontSize);
            }
        }

        private void MenuToggleMinimap_Click(object sender, RoutedEventArgs e)
        {
            ApplyMinimapToEditor(ViewModel.ShowMinimap);
        }

        private void MenuToggleWordWrap_Click(object sender, RoutedEventArgs e)
        {
            ApplyWordWrapToEditor(ViewModel.WordWrap);
        }

        private void MenuClearOutput_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearOutput();
        }

        private async void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "關於 AetherIDE",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "AetherIDE - 現代化 Windows 原生程式開發環境", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        new TextBlock { Text = "版本 1.0.0 (Windows App SDK 2.5 + .NET 8 / 10)" },
                        new TextBlock { Text = "GitHub: https://github.com/hankyleisplay/AetherIDE" },
                        new TextBlock { Text = "特色：Monaco 核心、多語言語法高亮、Aether AI 助理、範本庫、非同步即時編譯與終端機。" }
                    }
                },
                CloseButtonText = "確定",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void MenuShortcuts_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "常用快速鍵指南",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "• F5：直接執行目前檔案" },
                        new TextBlock { Text = "• Shift + F5：終止目前執行的程式" },
                        new TextBlock { Text = "• Ctrl + S：儲存檔案" },
                        new TextBlock { Text = "• Shift + Alt + F：格式化文件" },
                        new TextBlock { Text = "• Ctrl + N：建立新檔案" },
                        new TextBlock { Text = "• Ctrl + O：開啟檔案" },
                        new TextBlock { Text = "• Ctrl + Shift + O：開啟專案資料夾" },
                        new TextBlock { Text = "• Ctrl + W：關閉目前分頁" },
                        new TextBlock { Text = "• Ctrl + F：在目前檔案內尋找" },
                        new TextBlock { Text = "• Ctrl + Shift + F：全域搜尋" },
                        new TextBlock { Text = "• Ctrl + `：切換終端機面板" }
                    }
                },
                CloseButtonText = "關閉",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        #endregion

        #region Activity Bar & Sidebar Views

        private void HideAllSidebarViews()
        {
            ViewExplorer.Visibility = Visibility.Collapsed;
            ViewSearch.Visibility = Visibility.Collapsed;
            ViewAi.Visibility = Visibility.Collapsed;
            ViewSnippets.Visibility = Visibility.Collapsed;
            ViewRun.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;
            ExplorerHeaderActions.Visibility = Visibility.Collapsed;
        }

        private void BtnNavExplorer_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSidebarOpen = true;
            ViewModel.SelectedSidebarIndex = 0;
            SidebarTitleText.Text = "檔案總管";
            HideAllSidebarViews();
            ExplorerHeaderActions.Visibility = Visibility.Visible;
            ViewExplorer.Visibility = Visibility.Visible;
        }

        private void BtnNavSearch_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSidebarOpen = true;
            ViewModel.SelectedSidebarIndex = 1;
            SidebarTitleText.Text = "在工作區搜尋";
            HideAllSidebarViews();
            ViewSearch.Visibility = Visibility.Visible;
            SearchBox.Focus(FocusState.Programmatic);
        }

        private void BtnNavAi_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSidebarOpen = true;
            ViewModel.SelectedSidebarIndex = 2;
            SidebarTitleText.Text = "Aether AI 助理";
            HideAllSidebarViews();
            ViewAi.Visibility = Visibility.Visible;
            AiInputBox.Focus(FocusState.Programmatic);
        }

        private void BtnNavSnippets_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSidebarOpen = true;
            ViewModel.SelectedSidebarIndex = 3;
            SidebarTitleText.Text = "程式碼範本庫";
            HideAllSidebarViews();
            ViewSnippets.Visibility = Visibility.Visible;
        }

        private void BtnNavRun_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSidebarOpen = true;
            ViewModel.SelectedSidebarIndex = 4;
            SidebarTitleText.Text = "執行與除錯";
            HideAllSidebarViews();
            ViewRun.Visibility = Visibility.Visible;
        }

        private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsSidebarOpen = true;
            ViewModel.SelectedSidebarIndex = 5;
            SidebarTitleText.Text = "偏好設定";
            HideAllSidebarViews();
            ViewSettings.Visibility = Visibility.Visible;
        }

        private void BtnRefreshWorkspace_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ViewModel.WorkspacePath))
            {
                ViewModel.LoadWorkspace(ViewModel.WorkspacePath);
            }
        }

        private void WorkspaceTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is FileItem item && !item.IsDirectory)
            {
                ViewModel.OpenFile(item.FullPath);
            }
        }

        private void TreeItemOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is FileItem item && !item.IsDirectory)
            {
                ViewModel.OpenFile(item.FullPath);
            }
        }

        private void TreeItemReveal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is FileItem item)
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.FullPath}\"");
                }
                catch { }
            }
        }

        private void TreeItemCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is FileItem item)
            {
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText(item.FullPath);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
                ViewModel.StatusMessage = "已複製路徑到剪貼簿";
            }
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = ViewModel.ExecuteSearchAsync();
            }
        }

        private void BtnExecuteSearch_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.ExecuteSearchAsync();
        }

        private void SearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv && lv.SelectedItem is SearchResultItem item)
            {
                ViewModel.OpenFile(item.FilePath);
            }
        }

        #region AI Assistant Handlers

        private void BtnAiExplain_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.AskAiAsync("請詳細解釋目前的程式碼邏輯與關鍵結構");
        }

        private void BtnAiFindBugs_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.AskAiAsync("請進行靜態程式碼審查，尋找潛在 Bug 與邊界問題");
        }

        private void BtnAiOptimize_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.AskAiAsync("請針對目前程式碼提供效能最佳化與架構重構建議");
        }

        private void BtnAiGenerateTest_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.AskAiAsync("請針對目前檔案產生自動化單元測試代碼");
        }

        private void BtnAiSend_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.AskAiAsync();
        }

        private void AiInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = ViewModel.AskAiAsync();
            }
        }

        private void BtnInsertSnippet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SnippetItem snippet)
            {
                ViewModel.InsertSnippet(snippet);
            }
        }

        #endregion

        private async void BtnRunPython_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedTab != null)
            {
                await ViewModel.RunActiveFileAsync();
            }
            else
            {
                ViewModel.StatusMessage = "請先選擇要執行的檔案";
            }
        }

        private async void BtnRunNode_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedTab != null)
            {
                await ViewModel.RunActiveFileAsync();
            }
        }

        private async void BtnRunDotnet_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsBottomPanelOpen = true;
            BtnTabOutput_Click(this, new RoutedEventArgs());
            await ViewModel.ExecuteCommandAsync("dotnet run", ViewModel.WorkspacePath);
        }

        private async void BtnRunPowerShell_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedTab != null)
            {
                await ViewModel.RunActiveFileAsync();
            }
        }

        private void RadioTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is RadioButtons rb && rb.SelectedItem is RadioButton item)
            {
                bool isDark = item.Tag?.ToString() == "Dark";
                ViewModel.IsDarkMode = isDark;
                ApplyThemeToEditor(isDark);
                if (Content is FrameworkElement fe)
                {
                    fe.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
                }
            }
        }

        private void SliderFontSize_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            ApplyFontSizeToEditor((int)e.NewValue);
        }

        private void ToggleMinimap_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
            {
                ApplyMinimapToEditor(ts.IsOn);
            }
        }

        private void ToggleWordWrap_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
            {
                ApplyWordWrapToEditor(ts.IsOn);
            }
        }

        #endregion

        #region TabView & Editor Handlers

        private void EditorTabView_AddTabButtonClick(TabView sender, object args)
        {
            ViewModel.NewFile();
        }

        private void EditorTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (args.Item is EditorTabItem tab)
            {
                ViewModel.CloseTab(tab);
            }
        }

        private void EditorTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.SelectedTab != null)
            {
                UpdateEditorContent(ViewModel.SelectedTab.Content, ViewModel.SelectedTab.Language);
                ViewModel.CurrentLanguage = ViewModel.SelectedTab.Language;
            }
        }

        private async void TabMenuSave_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is EditorTabItem tab)
            {
                ViewModel.SelectedTab = tab;
                await ViewModel.SaveActiveFileAsync();
            }
        }

        private void TabMenuClose_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is EditorTabItem tab)
            {
                ViewModel.CloseTab(tab);
            }
        }

        private void TabMenuCloseOthers_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is EditorTabItem tab)
            {
                var toRemove = new List<EditorTabItem>();
                foreach (var t in ViewModel.OpenTabs)
                {
                    if (t != tab) toRemove.Add(t);
                }
                foreach (var t in toRemove)
                {
                    ViewModel.CloseTab(t);
                }
                ViewModel.SelectedTab = tab;
            }
        }

        private void TabMenuReveal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is EditorTabItem tab && !string.IsNullOrEmpty(tab.FilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{tab.FilePath}\"");
                }
                catch { }
            }
        }

        private void TabMenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.Tag is EditorTabItem tab && !string.IsNullOrEmpty(tab.FilePath))
            {
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText(tab.FilePath);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
                ViewModel.StatusMessage = "已複製路徑到剪貼簿";
            }
        }

        private void LanguageItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string lang && ViewModel.SelectedTab != null)
            {
                ViewModel.SelectedTab.Language = lang;
                ViewModel.CurrentLanguage = lang;
                UpdateEditorContent(ViewModel.SelectedTab.Content, lang);
            }
        }

        #endregion

        #region Bottom Panel Handlers

        private void BtnTabOutput_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedBottomPanelIndex = 0;
            OutputTextBox.Visibility = Visibility.Visible;
            TerminalTabGrid.Visibility = Visibility.Collapsed;
            ProblemsListView.Visibility = Visibility.Collapsed;
        }

        private void BtnTabTerminal_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedBottomPanelIndex = 1;
            OutputTextBox.Visibility = Visibility.Collapsed;
            TerminalTabGrid.Visibility = Visibility.Visible;
            ProblemsListView.Visibility = Visibility.Collapsed;
            TerminalInputBox.Focus(FocusState.Programmatic);
        }

        private void BtnTabProblems_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedBottomPanelIndex = 2;
            OutputTextBox.Visibility = Visibility.Collapsed;
            TerminalTabGrid.Visibility = Visibility.Collapsed;
            ProblemsListView.Visibility = Visibility.Visible;
        }

        private void TerminalInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = ViewModel.SendTerminalCommandAsync();
            }
        }

        private void BtnSendTerminal_Click(object sender, RoutedEventArgs e)
        {
            _ = ViewModel.SendTerminalCommandAsync();
        }

        #endregion
    }
}

