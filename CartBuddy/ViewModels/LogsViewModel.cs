using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CartBuddy.ViewModels;

public partial class LogsViewModel(ICartBuddyApi api) : ObservableObject
{
    public ObservableCollection<ApiLogEntry> Logs { get; } = [];

    public int TransactionCount =>
        Logs
            .Select(entry => entry.TransactionId == Guid.Empty ? entry.Id : entry.TransactionId)
            .Distinct()
            .Count();

    [ObservableProperty]
    private ApiLogEntry _selectedEntry;

    [RelayCommand]
    public async Task LoadLogs()
    {
        var entries = await api.GetLogs();

        Logs.Clear();
        foreach (var entry in entries)
        {
            Logs.Add(entry);
        }

        OnPropertyChanged(nameof(TransactionCount));
    }

    [RelayCommand]
    public async Task CopyEntry()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(SelectedEntry.Payload);
        await NotificationPopupService.Show("Copied payload to clipboard", NotificationPopupType.Success);
    }
}
