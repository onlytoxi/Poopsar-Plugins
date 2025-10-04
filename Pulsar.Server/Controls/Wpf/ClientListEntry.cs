using Pulsar.Server.Networking;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

#nullable enable

namespace Pulsar.Server.Controls.Wpf
{
    public sealed class ClientListEntry : INotifyPropertyChanged
    {
        private string _ip = string.Empty;
        private string _nickname = string.Empty;
        private string _tag = string.Empty;
        private string _userAtPc = string.Empty;
        private string _version = string.Empty;
        private string _status = string.Empty;
        private string _currentWindow = string.Empty;
        private string _userStatus = string.Empty;
        private string _countryWithCode = string.Empty;
        private string _country = string.Empty;
        private string _operatingSystem = string.Empty;
        private string _accountType = string.Empty;
        private bool _isFavorite;
        private string _toolTip = string.Empty;
        private int _imageIndex;

        public ClientListEntry(Client client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Client Client { get; }

        public string Ip
        {
            get => _ip;
            set => SetField(ref _ip, value);
        }

        public string Nickname
        {
            get => _nickname;
            set => SetField(ref _nickname, value);
        }

        public string Tag
        {
            get => _tag;
            set => SetField(ref _tag, value);
        }

        public string UserAtPc
        {
            get => _userAtPc;
            set => SetField(ref _userAtPc, value);
        }

        public string Version
        {
            get => _version;
            set => SetField(ref _version, value);
        }

        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        public string CurrentWindow
        {
            get => _currentWindow;
            set => SetField(ref _currentWindow, value);
        }

        public string UserStatus
        {
            get => _userStatus;
            set => SetField(ref _userStatus, value);
        }

        public string CountryWithCode
        {
            get => _countryWithCode;
            set => SetField(ref _countryWithCode, value);
        }

        public string Country
        {
            get => _country;
            set => SetField(ref _country, value);
        }

        public string OperatingSystem
        {
            get => _operatingSystem;
            set => SetField(ref _operatingSystem, value);
        }

        public string AccountType
        {
            get => _accountType;
            set => SetField(ref _accountType, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetField(ref _isFavorite, value);
        }

        public string ToolTip
        {
            get => _toolTip;
            set => SetField(ref _toolTip, value);
        }

        public int ImageIndex
        {
            get => _imageIndex;
            set => SetField(ref _imageIndex, value);
        }

        public Brush StatusBrush => string.Equals(Status, "Connected", StringComparison.OrdinalIgnoreCase)
            ? Brushes.LimeGreen
            : Brushes.White;

        public void UpdateStatusBrush()
        {
            OnPropertyChanged(nameof(StatusBrush));
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                if (propertyName == nameof(Status))
                {
                    UpdateStatusBrush();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
