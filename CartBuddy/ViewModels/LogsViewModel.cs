using CartBuddy.Services;
using CartBuddy.Models;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CartBuddy.ViewModels;

public partial class LogsViewModel(ICartBuddyApi api) : ObservableObject
{
    public ObservableCollection<LogTransactionGroup> TransactionGroups { get; } = [];

    [ObservableProperty]
    private LogTransactionGroup _selectedTransaction;

    [ObservableProperty]
    private bool _isDetailOpen;

    [RelayCommand]
    public async Task LoadLogs()
    {
        var entries = await api.GetLogs();
        var groups = entries
            .GroupBy(entry => entry.TransactionId == Guid.Empty ? entry.Id : entry.TransactionId)
            .OrderByDescending(group => group.Max(entry => entry.Timestamp))
            .Select(group => new LogTransactionGroup(group.Key, group));

        TransactionGroups.Clear();
        foreach (var group in groups)
        {
            TransactionGroups.Add(group);
        }
    }

    [RelayCommand]
    public void SelectEntry(ApiLogEntry entry)
    {
        var transaction = TransactionGroups.FirstOrDefault(group => group.Any(item => item.Id == entry.Id));
        if (transaction is null)
        {
            return;
        }

        SelectedTransaction = transaction;
        IsDetailOpen = true;
    }

    [RelayCommand]
    public async Task CopyTransaction()
    {
        if (SelectedTransaction is null)
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(SelectedTransaction.CopyText());
    }
}
