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
        if (!_viewModel.HasFrozenCleanedItems)
        {
            return true;
        }

        return await Application.Current!.MainPage!.DisplayAlert(
            "Close Search Terms",
            "Are you sure you want to close? Your categorized results will be lost.",
            "Close",
            "Cancel");
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