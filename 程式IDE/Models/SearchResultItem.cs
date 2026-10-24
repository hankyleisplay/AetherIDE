using CommunityToolkit.Mvvm.ComponentModel;

namespace 程式IDE.Models
{
    public class SearchResultItem : ObservableObject
    {
        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private string _fileName = string.Empty;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private int _lineNumber;
        public int LineNumber
        {
            get => _lineNumber;
            set => SetProperty(ref _lineNumber, value);
        }

        private string _lineContent = string.Empty;
        public string LineContent
        {
            get => _lineContent;
            set => SetProperty(ref _lineContent, value);
        }

        private string _matchText = string.Empty;
        public string MatchText
        {
            get => _matchText;
            set => SetProperty(ref _matchText, value);
        }
    }

    public class ProblemItem : ObservableObject
    {
        private string _severity = "Error"; // "Error", "Warning", "Info"
        public string Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private int _line;
        public int Line
        {
            get => _line;
            set => SetProperty(ref _line, value);
        }

        private int _column;
        public int Column
        {
            get => _column;
            set => SetProperty(ref _column, value);
        }

        public string SeverityGlyph => Severity switch
        {
            "Error" => "\uEA39",   // ErrorBadge
            "Warning" => "\uE7BA", // Warning
            _ => "\uE946"          // Info
        };

        public string SeverityColor => Severity switch
        {
            "Error" => "#E81123",
            "Warning" => "#FFB900",
            _ => "#0078D7"
        };
    }
}
