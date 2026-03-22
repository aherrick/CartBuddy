using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CartBuddy.ViewModels;

namespace CartBuddy;

public partial class SearchTermsPopup : AppPopup
{
    private readonly SearchTermsWorkflowViewModel _viewModel;

    public SearchTermsPopup(SearchTermsWorkflowViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        Opened += (_, _) => RestoreDraftIfEmpty();
        Closed += (_, _) => _viewModel.CancelPendingWork();
    }

    private void RestoreDraftIfEmpty()
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.RawItemsText))
        {
            return;
        }

        var saved = PreferencesService.SearchTermsDraft;
        if (!string.IsNullOrWhiteSpace(saved))
        {
            _viewModel.RawItemsText = saved;
        }
    }

    public IReadOnlyList<CategoryItem> ConfirmedTerms { get; private set; }

    protected override async Task<bool> ShouldCloseAsync()
    {
        if (!_viewModel.HasPendingEditChanges)
        {
            return true;
        }

        var currentPage = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        if (currentPage is null)
        {
            return true;
        }

        return await currentPage.DisplayAlertAsync(
            "Discard edits?",
            "Your draft is saved, but these changes have not been re-categorized or applied to the shopping list. Close anyway?",
            "Close",
            "Keep Editing");
    }

    private async void OnClearListClicked(object sender, EventArgs e)
    {
        if (!_viewModel.CanClearList)
        {
            return;
        }

        var currentPage = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        if (currentPage is null)
        {
            _viewModel.ClearDraft();
            return;
        }

        var shouldClear = await currentPage.DisplayAlertAsync(
            "Clear list?",
            "This removes the current draft from this editor so you can start over.",
            "Clear",
            "Cancel");

        if (!shouldClear)
        {
            return;
        }

        _viewModel.ClearDraft();
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        if (!_viewModel.HasFrozenCleanedItems)
        {
            return;
        }

        ConfirmedTerms = _viewModel.GetConfirmedTerms();
        await CloseAsync();
    }
}