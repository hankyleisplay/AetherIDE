using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace 程式IDE.Models
{
    public class FileItem : ObservableObject
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _fullPath = string.Empty;
        public string FullPath
        {
            get => _fullPath;
            set => SetProperty(ref _fullPath, value);
        }

        private bool _isDirectory;
        public bool IsDirectory
        {
            get => _isDirectory;
            set => SetProperty(ref _isDirectory, value);
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
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

        public ObservableCollection<FileItem> Children { get; } = new();

        public static FileItem FromDirectory(DirectoryInfo dir)
        {
            var item = new FileItem
            {
                Name = dir.Name,
                FullPath = dir.FullName,
                IsDirectory = true,
                IconGlyph = "\uE8B7", // Folder
                IconColor = "#FFE082" // Warm Yellow
            };
            return item;
        }

        public static FileItem FromFile(FileInfo file)
        {
            var (glyph, color) = GetIconInfo(file.Extension.ToLowerInvariant());
            var item = new FileItem
            {
                Name = file.Name,
                FullPath = file.FullName,
                IsDirectory = false,
                IconGlyph = glyph,
                IconColor = color
            };
            return item;
        }

        public static (string glyph, string color) GetIconInfo(string ext)
        {
            return ext switch
            {
                ".cs" => ("\uE943", "#9B4F96"),       // Purple for C#
                ".xaml" or ".xml" => ("\uE743", "#0078D7"), // Blue
                ".py" => ("\uE7BE", "#3572A5"),       // Blue/Python
                ".js" => ("\uE7BE", "#F7DF1E"),       // Yellow
                ".ts" => ("\uE7BE", "#3178C6"),       // TypeScript Blue
                ".json" => ("\uE7BE", "#FBC02D"),     // JSON Yellow
                ".html" or ".htm" => ("\uE774", "#E34F26"), // Orange
                ".css" => ("\uE790", "#2965F1"),      // Blue
                ".md" => ("\uE8A5", "#42A5F5"),       // Markdown Blue
                ".cpp" or ".c" or ".h" => ("\uE943", "#00599C"),
                ".sql" => ("\uE943", "#E38C00"),
                ".sh" or ".ps1" or ".bat" or ".cmd" => ("\uE756", "#4CAF50"), // Green terminal
                _ => ("\uE8A5", "#B0BEC5")             // Document generic
            };
        }
    }
}
