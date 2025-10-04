using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

#nullable enable

namespace Pulsar.Server.Controls.Wpf
{
    public partial class ClientsListView : UserControl
    {
        private readonly ObservableCollection<ClientListEntry> _entries = new();
        private readonly CollectionViewSource _collectionViewSource;
        private bool _groupByCountry;
        private Predicate<object>? _filter;
        private bool _suppressSelectionNotifications;

        public ClientsListView()
        {
            InitializeComponent();

            _collectionViewSource = new CollectionViewSource { Source = _entries };
            _collectionViewSource.Filter += OnCollectionFilter;
            ClientsView = _collectionViewSource.View;
            ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Country), ListSortDirection.Ascending));
            ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.IsFavorite), ListSortDirection.Descending));
            ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Nickname), ListSortDirection.Ascending));

            ToggleFavoriteCommand = new RelayCommand<ClientListEntry>(OnToggleFavorite);
            DataContext = this;

            ApplyTheme(Settings.DarkMode);
        }

        public ICollectionView ClientsView { get; }

        public ICommand ToggleFavoriteCommand { get; }

        public event EventHandler<IReadOnlyList<ClientListEntry>>? SelectionChanged;
        public event EventHandler<ClientListEntry>? ItemDoubleClicked;
        public event EventHandler<ClientListEntry>? FavoriteToggled;

        public IReadOnlyList<ClientListEntry> SelectedEntries => ClientsGrid.SelectedItems.Cast<ClientListEntry>().ToList();

        public ClientListEntry? GetEntryByClient(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return Dispatcher.Invoke(() => _entries.FirstOrDefault(e => ReferenceEquals(e.Client, client)));
        }

        public ClientListEntry AddOrUpdate(Client client, Action<ClientListEntry> updater)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (updater == null)
            {
                throw new ArgumentNullException(nameof(updater));
            }

            return Dispatcher.Invoke(() =>
            {
                var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Client, client));
                if (entry == null)
                {
                    entry = new ClientListEntry(client);
                    _entries.Add(entry);
                }

                updater(entry);
                return entry;
            });
        }

        public void Remove(Client client)
        {
            if (client == null)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var target = _entries.FirstOrDefault(e => ReferenceEquals(e.Client, client));
                if (target != null)
                {
                    _entries.Remove(target);
                }
            });
        }

        public void Clear()
        {
            Dispatcher.Invoke(() => _entries.Clear());
        }

        public void ApplyFilter(Predicate<ClientListEntry>? filter)
        {
            _filter = filter != null ? new Predicate<object>(o => filter((ClientListEntry)o)) : null;
            Dispatcher.Invoke(() => ClientsView.Refresh());
        }

        public void SetGroupByCountry(bool enabled)
        {
            _groupByCountry = enabled;
            Dispatcher.Invoke(UpdateGrouping);
        }

        public void SetSelectedClients(IEnumerable<Client> clients)
        {
            if (clients == null)
            {
                return;
            }

            var target = new HashSet<Client>(clients);

            Dispatcher.Invoke(() =>
            {
                _suppressSelectionNotifications = true;
                try
                {
                    ClientsGrid.SelectedItems.Clear();
                    foreach (var entry in _entries)
                    {
                        if (target.Contains(entry.Client))
                        {
                            ClientsGrid.SelectedItems.Add(entry);
                        }
                    }
                }
                finally
                {
                    _suppressSelectionNotifications = false;
                }
            });
        }

        public void RefreshSort()
        {
            Dispatcher.Invoke(() =>
            {
                using (ClientsView.DeferRefresh())
                {
                    ClientsView.SortDescriptions.Clear();
                    ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Country), ListSortDirection.Ascending));
                    ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.IsFavorite), ListSortDirection.Descending));
                    ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Nickname), ListSortDirection.Ascending));
                }
            });
        }

        public void RefreshItem(ClientListEntry entry)
        {
            Dispatcher.Invoke(() =>
            {
                entry.UpdateStatusBrush();
                ClientsView.Refresh();
            });
        }

        public void ApplyTheme(bool isDarkMode)
        {
            Dispatcher.Invoke(() =>
            {
                Resources["RowBackgroundBrush"] = CreateBrush(isDarkMode ? "#1E1E1E" : "#FFFFFF");
                Resources["RowAlternateBackgroundBrush"] = CreateBrush(isDarkMode ? "#232323" : "#F7F7F7");
                Resources["RowHoverBrush"] = CreateBrush(isDarkMode ? "#2E2E2E" : "#ECECEC");
                Resources["RowSelectedBrush"] = CreateBrush(isDarkMode ? "#1E3F73" : "#C4DAFF");
                Resources["RowSelectedInactiveBrush"] = CreateBrush(isDarkMode ? "#182F57" : "#E0ECFF");
                Resources["RowForegroundBrush"] = CreateBrush(isDarkMode ? "#FFFFFF" : "#1A1A1A");
                Resources["RowSelectedForegroundBrush"] = CreateBrush(isDarkMode ? "#FFFFFF" : "#1A1A1A");
                Resources["HeaderBackgroundBrush"] = CreateBrush(isDarkMode ? "#2A2A2A" : "#FFFFFF");
                Resources["HeaderForegroundBrush"] = CreateBrush(isDarkMode ? "#FFFFFF" : "#1A1A1A");
                Resources["GridBackgroundBrush"] = CreateBrush(isDarkMode ? "#141414" : "#FFFFFF");
                Resources["ScrollBarTrackBrush"] = CreateBrush(isDarkMode ? "#1E1E1E" : "#E5E5E5");
                Resources["ScrollBarThumbBrush"] = CreateBrush(isDarkMode ? "#444444" : "#B5B5B5");
                Resources["ScrollBarThumbHoverBrush"] = CreateBrush(isDarkMode ? "#5A5A5A" : "#9E9E9E");
                Resources["ScrollBarThumbPressedBrush"] = CreateBrush(isDarkMode ? "#737373" : "#7C7C7C");

                ClientsGrid.Background = (Brush)Resources["GridBackgroundBrush"];
                ClientsGrid.RowBackground = (Brush)Resources["RowBackgroundBrush"];
                ClientsGrid.AlternatingRowBackground = (Brush)Resources["RowAlternateBackgroundBrush"];
                ClientsGrid.Foreground = (Brush)Resources["RowForegroundBrush"];
            });
        }

        private void UpdateGrouping()
        {
            using (ClientsView.DeferRefresh())
            {
                ClientsView.GroupDescriptions.Clear();
                if (_groupByCountry)
                {
                    ClientsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ClientListEntry.Country)));
                }
            }
        }

        private void OnCollectionFilter(object sender, FilterEventArgs e)
        {
            if (_filter == null)
            {
                e.Accepted = true;
                return;
            }

            e.Accepted = _filter(e.Item);
        }

        private void OnToggleFavorite(ClientListEntry? entry)
        {
            if (entry == null)
            {
                return;
            }

            entry.IsFavorite = !entry.IsFavorite;
            RefreshSort();
            FavoriteToggled?.Invoke(this, entry);
        }

        private void ClientsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionNotifications)
            {
                return;
            }

            var selection = SelectedEntries;
            SelectionChanged?.Invoke(this, selection);
        }

        private void ClientsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClientsGrid.SelectedItem is ClientListEntry entry)
            {
                ItemDoubleClicked?.Invoke(this, entry);
            }
        }

        private void DataGridRow_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                if (!ClientsGrid.SelectedItems.Contains(row.Item))
                {
                    ClientsGrid.SelectedItem = row.Item;
                }

                ClientsGrid.Focus();
            }
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
