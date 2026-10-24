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
                    OnPropertyChanged(nameof(ActionEnabled));
                }
            }
        }

        private bool _isInstalled = true;
        public bool IsInstalled
        {
            get => _isInstalled;
            set => SetProperty(ref _isInstalled, value);
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

        private string _actionTitle = "執行操作";
        public string ActionTitle
        {
            get => _actionTitle;
            set => SetProperty(ref _actionTitle, value);
        }

        private string _actionGlyph = "\uE768";
        public string ActionGlyph
        {
            get => _actionGlyph;
            set => SetProperty(ref _actionGlyph, value);
        }

        private string _actionTooltip = string.Empty;
        public string ActionTooltip
        {
            get => _actionTooltip;
            set => SetProperty(ref _actionTooltip, value);
        }

        private string _downloads = "10K+";
        public string Downloads
        {
            get => _downloads;
            set => SetProperty(ref _downloads, value);
        }

        private string _rating = "★ 4.8";
        public string Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        private string _settingsSummary = string.Empty;
        public string SettingsSummary
        {
            get => _settingsSummary;
            set => SetProperty(ref _settingsSummary, value);
        }

        public bool ActionEnabled => IsEnabled && IsInstalled;
        public string StatusText => IsEnabled ? "已啟用" : "已停用";
        public string StatusColor => IsEnabled ? "#107C41" : "#888888";
    }
}

