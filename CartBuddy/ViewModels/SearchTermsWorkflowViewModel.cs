using System.Collections.ObjectModel;
using CartBuddy.Models;
using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartBuddy.ViewModels;

public partial class SearchTermsWorkflowViewModel : ObservableObject
{
    private readonly ICartBuddyApi _api;
    private CancellationTokenSource _cleanupCts = new();
    private PreparationPhase _phaseBeforeEdit = PreparationPhase.Edit;
    private string _editBaselineText = string.Empty;

    public SearchTermsWorkflowViewModel(ICartBuddyApi api)
    {
        _api = api;
        FrozenCleanedItems.CollectionChanged += (_, _) => UpdateWorkflowState();
    }

    [ObservableProperty]
    private string _rawItemsText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private PreparationPhase _currentPhase = PreparationPhase.Edit;

    public ObservableCollection<CategoryItem> FrozenCleanedItems { get; } = [];

    public bool IsEditPhase => CurrentPhase == PreparationPhase.Edit;

    public bool IsCleanupPreviewPhase => CurrentPhase == PreparationPhase.CleanupPreview;

    public bool HasItemsText => !string.IsNullOrWhiteSpace(RawItemsText);

    public bool HasFrozenCleanedItems => FrozenCleanedItems.Count > 0;

    public bool CanClearList =>
        IsEditPhase
        && (HasItemsText || HasFrozenCleanedItems);

    public bool HasPendingEditChanges =>
        IsEditPhase
        && HasFrozenCleanedItems
        && !string.Equals(
            NormalizeEditableText(RawItemsText),
            NormalizeEditableText(_editBaselineText),
            StringComparison.Ordinal
        );

    public bool CanCancelEdit =>
        CurrentPhase == PreparationPhase.Edit
        && HasFrozenCleanedItems;

    public string WorkflowSummary
    {
        get
        {
            var itemCount = CurrentPhase == PreparationPhase.CleanupPreview
                ? FrozenCleanedItems.Count
                : ParseDraftLines(RawItemsText).Count;

            return itemCount == 0 ? "Enter one item per line" : $"{itemCount} terms";
        }
    }

    public void LoadConfirmedTerms(IEnumerable<CategoryItem> terms)
    {
        CancelPendingWork();
        FrozenCleanedItems.Clear();

        foreach (var term in terms)
        {
            if (string.IsNullOrWhiteSpace(term?.Item))
            {
                continue;
            }

            FrozenCleanedItems.Add(term.Clone());
        }

        RawItemsText = BuildAnnotatedItemsText(FrozenCleanedItems);
        CurrentPhase = HasFrozenCleanedItems ? PreparationPhase.CleanupPreview : PreparationPhase.Edit;
        _phaseBeforeEdit = PreparationPhase.Edit;
        _editBaselineText = string.Empty;
        IsBusy = false;
        OnPropertyChanged(nameof(HasPendingEditChanges));
    }

    public IReadOnlyList<CategoryItem> GetConfirmedTerms() =>
        [.. FrozenCleanedItems.Select(item => item.Clone())];

    public void ClearDraft()
    {
        InvalidateFrozenList();
        RawItemsText = string.Empty;
        CurrentPhase = PreparationPhase.Edit;
        OnPropertyChanged(nameof(CanClearList));
        OnPropertyChanged(nameof(HasPendingEditChanges));
    }

    public void CancelPendingWork()
    {
        var previous = _cleanupCts;
        _cleanupCts = new CancellationTokenSource();

        previous.Cancel();
        previous.Dispose();
        IsBusy = false;
    }

    partial void OnCurrentPhaseChanged(PreparationPhase value)
    {
        OnPropertyChanged(nameof(IsEditPhase));
        OnPropertyChanged(nameof(IsCleanupPreviewPhase));
        OnPropertyChanged(nameof(CanClearList));
        OnPropertyChanged(nameof(HasPendingEditChanges));
        OnPropertyChanged(nameof(CanCancelEdit));
        OnPropertyChanged(nameof(WorkflowSummary));
    }

    partial void OnRawItemsTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasItemsText));
        OnPropertyChanged(nameof(CanClearList));
        OnPropertyChanged(nameof(HasPendingEditChanges));
        OnPropertyChanged(nameof(WorkflowSummary));

        if (CurrentPhase == PreparationPhase.Edit)
        {
            PreferencesService.SearchTermsDraft = value;
        }
    }

    [RelayCommand]
    private async Task RunAiCleanup()
    {
        if (string.IsNullOrWhiteSpace(RawItemsText))
        {
            await NotificationPopupService.Show("Paste a list first", NotificationPopupType.Info);
            return;
        }

        try
        {
            await ExecuteBusyAsync(async () =>
            {
                var previous = _cleanupCts;
                _cleanupCts = new CancellationTokenSource();
                previous.Cancel();
                previous.Dispose();
                var ct = _cleanupCts.Token;

                var rawTerms = ParseDraftLines(RawItemsText);
                var cleanedItems = await _api.CleanupList(new CleanupRequest { Items = rawTerms }, ct);
                ct.ThrowIfCancellationRequested();

                ApplyCleanupPreview(rawTerms, cleanedItems);
                RawItemsText = BuildAnnotatedItemsText(FrozenCleanedItems);
                CurrentPhase = PreparationPhase.CleanupPreview;
                _phaseBeforeEdit = PreparationPhase.Edit;
                _editBaselineText = string.Empty;
                OnPropertyChanged(nameof(HasPendingEditChanges));
            });
        }
        catch (OperationCanceledException)
        {
            // popup closed or cleanup restarted — ignore
        }
        catch (Exception ex)
        {
            await NotificationPopupService.Show($"Cleanup failed: {ex.Message}", NotificationPopupType.Error);
        }
    }

    [RelayCommand]
    private void EditList()
    {
        if (!IsCleanupPreviewPhase)
        {
            return;
        }

        _phaseBeforeEdit = CurrentPhase;
        _editBaselineText = BuildAnnotatedItemsText(FrozenCleanedItems);
        RawItemsText = _editBaselineText;
        CurrentPhase = PreparationPhase.Edit;
        OnPropertyChanged(nameof(HasPendingEditChanges));
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (!CanCancelEdit)
        {
            return;
        }

        RawItemsText = _editBaselineText;
        CurrentPhase = PreparationPhase.CleanupPreview;
        _phaseBeforeEdit = PreparationPhase.Edit;
        OnPropertyChanged(nameof(HasPendingEditChanges));
    }

    private static List<string> ParseDraftLines(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return
        [
            .. input
                .Split(
                    ['\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        IsBusy = true;

        try
        {
            await action();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyCleanupPreview(List<string> rawTerms, IReadOnlyList<CategoryItem> cleanedItems)
    {
        FrozenCleanedItems.Clear();

        for (var i = 0; i < rawTerms.Count; i++)
        {
            var cleanedItem = i < cleanedItems.Count ? cleanedItems[i] : null;
            FrozenCleanedItems.Add(new CategoryItem
            {
                Item = string.IsNullOrWhiteSpace(cleanedItem?.Item) ? rawTerms[i] : cleanedItem.Item.Trim(),
                Category = string.IsNullOrWhiteSpace(cleanedItem?.Category) ? "other" : cleanedItem.Category.Trim(),
            });
        }
    }

    private void InvalidateFrozenList()
    {
        FrozenCleanedItems.Clear();
        _phaseBeforeEdit = PreparationPhase.Edit;
        _editBaselineText = string.Empty;
    }

    private void UpdateWorkflowState()
    {
        OnPropertyChanged(nameof(CanClearList));
        OnPropertyChanged(nameof(HasFrozenCleanedItems));
        OnPropertyChanged(nameof(CanCancelEdit));
        OnPropertyChanged(nameof(WorkflowSummary));
    }

    private static string BuildAnnotatedItemsText(IEnumerable<CategoryItem> items) =>
        string.Join(Environment.NewLine, items.Select(item => $"{item.Item} ({item.Category})"));

    private static string NormalizeEditableText(string value) =>
        string.Join(
            "\n",
            value
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );

    public enum PreparationPhase
    {
        Edit,
        CleanupPreview,
    }
}