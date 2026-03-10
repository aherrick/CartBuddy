using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CartBuddy.Models;

public partial class ProductMatch : ObservableObject
{
    [ObservableProperty]
    private string _query;

    [ObservableProperty]
    private string _productId;

    [ObservableProperty]
    private string _upc;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private string _brand;

    [ObservableProperty]
    private string _size;

    [ObservableProperty]
    private string _imageUrl;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private decimal _regularPrice;

    [ObservableProperty]
    private bool _hasPromo;

    [ObservableProperty]
    private string _promoEndDate;

    public bool HasSale => HasPromo && RegularPrice > Price;

    public string RegularPriceDisplay => HasSale ? $"(Reg ${RegularPrice:F2})" : string.Empty;

    public string PromoEndDisplay
    {
        get
        {
            if (!HasSale || string.IsNullOrWhiteSpace(PromoEndDate))
            {
                return string.Empty;
            }

            return DateTime.TryParse(PromoEndDate, out var date)
                ? $"Until {date:M/d}"
                : PromoEndDate;
        }
    }

    partial void OnPriceChanged(decimal value)
    {
        OnPricingChanged();
    }

    partial void OnRegularPriceChanged(decimal value)
    {
        OnPricingChanged();
    }

    partial void OnHasPromoChanged(bool value)
    {
        OnPricingChanged();
    }

    partial void OnPromoEndDateChanged(string value)
    {
        OnPropertyChanged(nameof(PromoEndDisplay));
    }

    private void OnPricingChanged()
    {
        OnPropertyChanged(nameof(HasSale));
        OnPropertyChanged(nameof(RegularPriceDisplay));
        OnPropertyChanged(nameof(PromoEndDisplay));
    }
}

public partial class CartLine : ObservableObject
{
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
    private int _quantity;

    public decimal LineTotal => Price * Quantity;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }
}

public partial class SearchGroup(string query, int totalCount, int pageSize) : ObservableObject
{
    public string Query { get; } = query;

    public int PageSize { get; } = pageSize;

    public ObservableCollection<ProductMatch> Matches { get; } = [];

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private int _totalCount = totalCount;

    [ObservableProperty]
    private int _loadedCount;

    [ObservableProperty]
    private bool _isCompleted;

    public bool HasMatches => Matches.Count > 0;

    public bool IsEmpty => Matches.Count == 0;

    public bool HasMore => LoadedCount < TotalCount;

    public string PageSummary => TotalCount == 0 ? "No matches" : $"{LoadedCount}/{TotalCount}";

    public string ToggleText => IsExpanded ? "Collapse" : "View";

    public string ToggleIconGlyph => IsExpanded ? "\uf078" : "\uf054";

    public void AddMatches(IEnumerable<ProductMatch> matches)
    {
        foreach (var match in matches)
        {
            Matches.Add(match);
        }

        LoadedCount = Matches.Count;
        OnPropertyChanged(nameof(HasMatches));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(PageSummary));
    }

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(ToggleIconGlyph));
    }

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(PageSummary));
    }

    partial void OnLoadedCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(PageSummary));
    }

    partial void OnIsCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(PageSummary));
    }
}

public class ProductSearchPage(List<ProductMatch> results, int totalCount)
{
    public List<ProductMatch> Results { get; } = results;

    public int TotalCount { get; } = totalCount;
}
