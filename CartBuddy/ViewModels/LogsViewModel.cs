using CartBuddy.Services;
using CartBuddy.Models;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Extensions;

namespace CartBuddy.ViewModels;

public partial class LogsViewModel(ICartBuddyApi api, INotificationPopupService notifications) : ObservableObject
{
    public ObservableCollection<ApiLogEntry> Logs { get; } = [];
    public ObservableCollection<LogTransactionGroup> TransactionGroups { get; } = [];

    public int TransactionCount => TransactionGroups.Count;

    [ObservableProperty]
    private ApiLogEntry _selectedEntry;

    [RelayCommand]
    public async Task LoadLogs()
    {
        var entries = await api.GetLogs();
        var groups = entries
            .GroupBy(entry => entry.TransactionId == Guid.Empty ? entry.Id : entry.TransactionId)
            .OrderByDescending(group => group.Max(entry => entry.Timestamp))
            .Select(group => new LogTransactionGroup(group.Key, group));

        Logs.Clear();
        foreach (var entry in entries)
        {
            Logs.Add(entry);
        }

        TransactionGroups.Clear();
        foreach (var group in groups)
        {
            TransactionGroups.Add(group);
        }

        OnPropertyChanged(nameof(TransactionCount));
    }

    [RelayCommand]
    public async Task SelectEntry(ApiLogEntry entry)
    {
        if (entry is null)
        {
            return;
        }

        SelectedEntry = entry;
        
        var page = Shell.Current?.CurrentPage;
        if (page is not null)
        {
            await page.ShowPopupAsync(new LogDetailPopup(this), new CommunityToolkit.Maui.PopupOptions());
        }
    }

    [RelayCommand]
    public async Task CopyEntry()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(SelectedEntry.Payload);
        await notifications.Show("Copied payload to clipboard", NotificationPopupType.Success);
    }
}
