using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace 程式IDE.Models
{
    public class EditorTabItem : ObservableObject
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        private string _title = "Untitled";
        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private string _content = string.Empty;
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (SetProperty(ref _isDirty, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        private string _language = "plaintext";
        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        private string _iconGlyph = "\uE8A5";
        public string IconGlyph
        {
            get => _iconGlyph;
            set => SetProperty(ref _iconGlyph, value);
        }

        private string _iconColor = "#CCCCCC";
        public string IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        private int _line = 1;
        public int Line
        {
            get => _line;
            set => SetProperty(ref _line, value);
        }

        private int _column = 1;
        public int Column
        {
            get => _column;
            set => SetProperty(ref _column, value);
        }

        public string DisplayTitle => IsDirty ? $"{Title} •" : Title;

        public static string DetectLanguage(string? path)
        {
            if (string.IsNullOrEmpty(path)) return "plaintext";
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".cs" => "csharp",
                ".py" => "python",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".html" or ".htm" => "html",
                ".css" => "css",
                ".json" => "json",
                ".xml" or ".xaml" => "xml",
                ".md" => "markdown",
                ".cpp" or ".cc" or ".cxx" => "cpp",
                ".c" or ".h" => "c",
                ".sql" => "sql",
                ".ps1" => "powershell",
                ".bat" or ".cmd" => "bat",
                ".sh" => "shell",
                ".yaml" or ".yml" => "yaml",
                _ => "plaintext"
            };
        }

        public static EditorTabItem CreateNew(string title = "Untitled-1", string content = "", string? filePath = null)
        {
            var lang = DetectLanguage(filePath ?? title);
            var (glyph, color) = FileItem.GetIconInfo(Path.GetExtension(filePath ?? title).ToLowerInvariant());
            return new EditorTabItem
            {
                Title = title,
                FilePath = filePath,
                Content = content,
                Language = lang,
                IconGlyph = glyph,
                IconColor = color,
                IsDirty = false
            };
        }
    }
}
