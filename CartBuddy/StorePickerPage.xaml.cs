using CartBuddy.ViewModels;

namespace CartBuddy;

public partial class StorePickerPage : ContentPage
{
    private readonly StorePickerViewModel _viewModel;

    public StorePickerPage(StorePickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadSavedZip();
    }
}