using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace 程式IDE.Models
{
    public class McpServerConfig : ObservableObject
    {
        private string _id = System.Guid.NewGuid().ToString();
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

        private string _command = string.Empty;
        public string Command
        {
            get => _command;
            set => SetProperty(ref _command, value);
        }

        private string _arguments = string.Empty;
        public string Arguments
        {
            get => _arguments;
            set => SetProperty(ref _arguments, value);
        }

        private string _transport = "stdio"; // stdio or sse
        public string Transport
        {
            get => _transport;
            set => SetProperty(ref _transport, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private string _status = "已連線"; // 已連線, 已停止, 錯誤
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public string StatusColor => Status switch
        {
            "已連線" => "#107C41", // Green
            "連線中" => "#0078D7", // Blue
            "錯誤" => "#E81123",   // Red
            _ => "#888888"         // Gray
        };

        public ObservableCollection<McpToolItem> Tools { get; } = new();

        public int ToolsCount => Tools.Count;
    }

    public class McpToolItem : ObservableObject
    {
        private string _serverName = string.Empty;
        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _parametersSummary = string.Empty;
        public string ParametersSummary
        {
            get => _parametersSummary;
            set => SetProperty(ref _parametersSummary, value);
        }
    }
}

