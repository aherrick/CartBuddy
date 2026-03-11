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

public class ProductSearchPage(List<ProductMatch> results, int totalCount)
{
    public List<ProductMatch> Results { get; } = results;

    public int TotalCount { get; } = totalCount;
}
