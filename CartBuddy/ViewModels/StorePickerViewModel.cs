using System.Collections.ObjectModel;
using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartBuddy.ViewModels;

public partial class StorePickerViewModel(
    ICartBuddyApi api,
    PreferencesService preferences
) : ObservableObject
{
    [ObservableProperty]
    private string _zipCode;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage;

    public bool CanNavigateBack => preferences.HasStore;

    public ObservableCollection<LocationInfo> Stores { get; } = [];

    public void LoadSavedZip()
    {
        ZipCode = preferences.ZipCode;
        StatusMessage = preferences.HasStore
            ? $"Current store: {preferences.StoreName}"
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

        try
        {
            var response = await api.SearchLocations(ZipCode);
            var locations = response.Locations ?? [];
            foreach (var location in locations)
            {
                Stores.Add(location);
            }

            preferences.ZipCode = ZipCode;
            StatusMessage = $"Found {locations.Count} stores";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectStore(LocationInfo store)
    {
        preferences.StoreId = store.LocationId;
        preferences.StoreName = string.IsNullOrWhiteSpace(store.Address)
            ? store.Name
            : $"{store.Name} - {store.Address}";
        OnPropertyChanged(nameof(CanNavigateBack));
        await Shell.Current.GoToAsync("..");
    }
}