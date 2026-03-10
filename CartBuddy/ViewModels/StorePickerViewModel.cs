using System.Collections.ObjectModel;
using CartBuddy.Data.Models;
using CartBuddy.Models;
using CartBuddy.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartBuddy.ViewModels;

public partial class StorePickerViewModel(
    KrogerApiService krogerApi,
    PreferencesService preferences
) : ObservableObject
{
    [ObservableProperty]
    private string _zipCode;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage;

    public ObservableCollection<KrogerLocation> Stores { get; } = [];

    public void LoadSavedZip() => ZipCode = preferences.ZipCode;

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
            var locations = await krogerApi.SearchLocations(ZipCode);
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
    private async Task SelectStore(KrogerLocation store)
    {
        preferences.StoreId = store.LocationId;
        preferences.StoreName = store.Address is not null
            ? $"{store.Name} - {store.Address.AddressLine1}"
            : store.Name;
        await Shell.Current.GoToAsync("..");
    }
}