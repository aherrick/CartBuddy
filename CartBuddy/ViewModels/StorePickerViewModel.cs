using System.Collections.ObjectModel;
using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartBuddy.ViewModels;

public partial class StorePickerViewModel(ICartBuddyApi api) : ObservableObject
{
    [ObservableProperty]
    private string _zipCode;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage;

    public bool CanNavigateBack => PreferencesService.HasStore;
    public bool HasStores => Stores.Count > 0;
    public bool IsIdle => !IsBusy && Stores.Count == 0;

    public ObservableCollection<LocationInfo> Stores { get; } = [];

    public void LoadSavedZip()
    {
        ZipCode = PreferencesService.ZipCode;
        StatusMessage = PreferencesService.HasStore
            ? $"Current store: {PreferencesService.StoreName}"
            : "Pick a store before searching or checking out.";
        OnPropertyChanged(nameof(CanNavigateBack));
    }

    [RelayCommand]
    private async Task SearchStores()
    {
        if (string.IsNullOrWhiteSpace(ZipCode) || ZipCode.Length < 5)
        {
            StatusMessage = "Enter a valid zip code";
            return;
        }

        IsBusy = true;
        StatusMessage = "Searching stores...";
        Stores.Clear();
        OnPropertyChanged(nameof(HasStores));
        OnPropertyChanged(nameof(IsIdle));

        try
        {
            var response = await api.SearchLocations(ZipCode);
            var locations = response.Locations ?? [];
            foreach (var location in locations)
            {
                Stores.Add(location);
            }

            PreferencesService.ZipCode = ZipCode;
            StatusMessage = $"Found {locations.Count} stores";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasStores));
            OnPropertyChanged(nameof(IsIdle));
        }
    }

    [RelayCommand]
    private async Task SelectStore(LocationInfo store)
    {
        PreferencesService.StoreId = store.LocationId;
        PreferencesService.StoreName = string.IsNullOrWhiteSpace(store.Address)
            ? store.Name
            : $"{store.Name} - {store.Address}";
        OnPropertyChanged(nameof(CanNavigateBack));
        await Shell.Current.GoToAsync("..");
    }
}
