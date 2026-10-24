using CommunityToolkit.Mvvm.ComponentModel;

namespace 程式IDE.Models
{
    public class PluginItem : ObservableObject
    {
        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _version = "1.0.0";
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private string _author = string.Empty;
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _category = "開發工具";
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        private string _iconGlyph = "\uEA86";
        public string IconGlyph
        {
            get => _iconGlyph;
            set => SetProperty(ref _iconGlyph, value);
        }

        private string _iconColor = "#0078D7";
        public string IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        public string StatusText => IsEnabled ? "已啟用" : "已停用";
        public string StatusColor => IsEnabled ? "#107C41" : "#888888";
    }
}

