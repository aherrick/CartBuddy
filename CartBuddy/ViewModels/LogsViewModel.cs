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

    [RelayCommand]
    public async Task LoadLogs()
    {
        var entries = await api.GetLogs();
        Logs.Clear();
        foreach (var entry in entries)
        {
            Logs.Add(entry);
        }
    }

    public void SelectEntry(ApiLogEntry entry)
    {
        SelectedEntry = entry;
        IsDetailOpen = true;
    }
}
