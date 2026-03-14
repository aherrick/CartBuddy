using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CartBuddy.Models;

public partial class ProductMatch : ObservableObject
{
    [ObservableProperty]
    private string _query;

    [ObservableProperty]
    private string _upc;

    [NotifyPropertyChangedFor(nameof(DisplayBrand))]
    [ObservableProperty]
    private string _description;

    [NotifyPropertyChangedFor(nameof(DisplayBrand))]
    [ObservableProperty]
    private string _brand;

    [ObservableProperty]
    private string _size;

    [ObservableProperty]
    private string _imageUrl;

    [NotifyPropertyChangedFor(nameof(HasSale))]
    [NotifyPropertyChangedFor(nameof(RegularPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(PromoEndDisplay))]
    [ObservableProperty]
    private decimal _price;

    [NotifyPropertyChangedFor(nameof(HasSale))]
    [NotifyPropertyChangedFor(nameof(RegularPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(PromoEndDisplay))]
    [ObservableProperty]
    private decimal _regularPrice;

    [NotifyPropertyChangedFor(nameof(HasSale))]
    [NotifyPropertyChangedFor(nameof(RegularPriceDisplay))]
    [NotifyPropertyChangedFor(nameof(PromoEndDisplay))]
    [ObservableProperty]
    private bool _hasPromo;

    [NotifyPropertyChangedFor(nameof(PromoEndDisplay))]
    [ObservableProperty]
    private string _promoEndDate;

    public bool IsNoResult { get; set; }

    public bool HasSale => HasPromo && RegularPrice > Price;

    public string DisplayBrand =>
        string.IsNullOrWhiteSpace(Brand)
        || Description.StartsWith(Brand, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : Brand;

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

}

public partial class CartLine : ObservableObject
{
    [ObservableProperty]
    private string _upc;

    [NotifyPropertyChangedFor(nameof(DisplayBrand))]
    [ObservableProperty]
    private string _description;

    [NotifyPropertyChangedFor(nameof(DisplayBrand))]
    [ObservableProperty]
    private string _brand;

    [NotifyPropertyChangedFor(nameof(DetailDisplay))]
    [ObservableProperty]
    private string _size;

    [ObservableProperty]
    private string _imageUrl;

    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [ObservableProperty]
    private decimal _price;

    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [ObservableProperty]
    private int _quantity;

    public decimal LineTotal => Price * Quantity;

    public HashSet<string> SourceQueries { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string DisplayBrand =>
        string.IsNullOrWhiteSpace(Brand)
        || Description.StartsWith(Brand, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : Brand;

    public string DetailDisplay => Size;

}

public class SearchGroup(string query, int totalCount, int pageSize) : ObservableCollection<ProductMatch>
{
    private int _totalCount = totalCount;
    private int _loadedCount;
    private bool _isCompleted;

    public string Query { get; } = query;

    public int PageSize { get; } = pageSize;

    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value, nameof(TotalCount), nameof(HasMore), nameof(PageSummary));
    }

    public int LoadedCount
    {
        get => _loadedCount;
        private set => SetProperty(ref _loadedCount, value, nameof(LoadedCount), nameof(HasMore), nameof(PageSummary));
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value, nameof(IsCompleted));
    }

    public bool HasMore => LoadedCount < TotalCount;

    public string PageSummary => TotalCount == 0 ? "No matches" : $"{LoadedCount}/{TotalCount}";

    public void AddMatches(IEnumerable<ProductMatch> matches)
    {
        foreach (var match in matches)
        {
            Add(match);
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        LoadedCount = Count;
    }

    private void SetProperty<T>(ref T field, T value, params string[] propertyNames)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}

public record ProductSearchPage(List<ProductMatch> Results, int TotalCount);
