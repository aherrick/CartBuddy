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

    public string PromoEndDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PromoEndDate))
            {
                return string.Empty;
            }

            return DateTime.TryParse(PromoEndDate, out var date)
                ? $"Until {date:M/d}"
                : PromoEndDate;
        }
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

    private partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }
}

public partial class SearchGroup : ObservableObject
{
    public SearchGroup(string query, int totalCount, int pageSize)
    {
        Query = query;
        TotalCount = totalCount;
        PageSize = pageSize;
        Matches = [];
    }

    public string Query { get; }

    public int PageSize { get; }

    public ObservableCollection<ProductMatch> Matches { get; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private int _totalCount;

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

    private partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(ToggleIconGlyph));
    }

    private partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(PageSummary));
    }

    private partial void OnLoadedCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(PageSummary));
    }

    private partial void OnIsCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(PageSummary));
    }
}

public class ProductSearchPage
{
    public ProductSearchPage(List<ProductMatch> results, int totalCount)
    {
        Results = results;
        TotalCount = totalCount;
    }

    public List<ProductMatch> Results { get; }

    public int TotalCount { get; }
}