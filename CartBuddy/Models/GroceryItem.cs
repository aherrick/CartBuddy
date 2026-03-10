using CommunityToolkit.Mvvm.ComponentModel;

namespace CartBuddy.Models;

public partial class GroceryItem : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _productId;

    [ObservableProperty]
    private string _upc;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private string _imageUrl;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private bool _isResolved;

    [ObservableProperty]
    private bool _isCheckedOut;
}