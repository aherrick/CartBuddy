using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CartBuddy.ViewModels;

public partial class LogsViewModel(ICartBuddyApi api) : ObservableObject
{
    public ObservableCollection<ApiLogEntry> Logs { get; } = [];

    [ObservableProperty]
    private ApiLogEntry _selectedEntry;

    [ObservableProperty]
    private bool _isDetailOpen;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    public async Task LoadLogs()
    {
        IsLoading = true;
        try
        {
            var entries = await api.GetLogs();
            Logs.Clear();
            foreach (var entry in entries)
            {
                Logs.Add(entry);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SelectEntry(ApiLogEntry entry)
    {
        SelectedEntry = entry;
        IsDetailOpen = true;
    }
}
