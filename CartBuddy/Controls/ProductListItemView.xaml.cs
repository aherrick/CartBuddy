using System.Windows.Input;

namespace CartBuddy.Controls;

public partial class ProductListItemView : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(ProductListItemView), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(ProductListItemView), string.Empty);

    public static readonly BindableProperty DetailProperty =
        BindableProperty.Create(nameof(Detail), typeof(string), typeof(ProductListItemView), string.Empty);

    public static readonly BindableProperty ImageUrlProperty =
        BindableProperty.Create(nameof(ImageUrl), typeof(string), typeof(ProductListItemView), string.Empty);

    public static readonly BindableProperty PriceTextProperty =
        BindableProperty.Create(nameof(PriceText), typeof(string), typeof(ProductListItemView), string.Empty);

    public static readonly BindableProperty SecondaryPriceTextProperty =
        BindableProperty.Create(nameof(SecondaryPriceText), typeof(string), typeof(ProductListItemView), string.Empty);

    public static readonly BindableProperty SecondaryPriceStrikethroughProperty =
        BindableProperty.Create(nameof(SecondaryPriceStrikethrough), typeof(bool), typeof(ProductListItemView), false);

    public static readonly BindableProperty AccentTextProperty =
        BindableProperty.Create(nameof(AccentText), typeof(string), typeof(ProductListItemView), string.Empty);

    public static readonly BindableProperty PriceColorProperty =
        BindableProperty.Create(nameof(PriceColor), typeof(Color), typeof(ProductListItemView), Colors.Black);

    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(ProductListItemView), Colors.Black);

    public static readonly BindableProperty ShowPriceBadgeProperty =
        BindableProperty.Create(nameof(ShowPriceBadge), typeof(bool), typeof(ProductListItemView), false);

    public static readonly BindableProperty PriceBadgeColorProperty =
        BindableProperty.Create(nameof(PriceBadgeColor), typeof(Color), typeof(ProductListItemView), Colors.Transparent);

    public static readonly BindableProperty ItemCommandProperty =
        BindableProperty.Create(nameof(ItemCommand), typeof(ICommand), typeof(ProductListItemView));

    public static readonly BindableProperty ItemCommandParameterProperty =
        BindableProperty.Create(nameof(ItemCommandParameter), typeof(object), typeof(ProductListItemView));

    public static readonly BindableProperty ShowQuantityControlsProperty =
        BindableProperty.Create(nameof(ShowQuantityControls), typeof(bool), typeof(ProductListItemView), false);

    public static readonly BindableProperty QuantityProperty =
        BindableProperty.Create(nameof(Quantity), typeof(int), typeof(ProductListItemView), 0);

    public static readonly BindableProperty IncreaseCommandProperty =
        BindableProperty.Create(nameof(IncreaseCommand), typeof(ICommand), typeof(ProductListItemView));

    public static readonly BindableProperty DecreaseCommandProperty =
        BindableProperty.Create(nameof(DecreaseCommand), typeof(ICommand), typeof(ProductListItemView));

    public static readonly BindableProperty QuantityCommandParameterProperty =
        BindableProperty.Create(nameof(QuantityCommandParameter), typeof(object), typeof(ProductListItemView));

    public ProductListItemView()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public string PriceText
    {
        get => (string)GetValue(PriceTextProperty);
        set => SetValue(PriceTextProperty, value);
    }

    public string SecondaryPriceText
    {
        get => (string)GetValue(SecondaryPriceTextProperty);
        set => SetValue(SecondaryPriceTextProperty, value);
    }

    public bool SecondaryPriceStrikethrough
    {
        get => (bool)GetValue(SecondaryPriceStrikethroughProperty);
        set => SetValue(SecondaryPriceStrikethroughProperty, value);
    }

    public string AccentText
    {
        get => (string)GetValue(AccentTextProperty);
        set => SetValue(AccentTextProperty, value);
    }

    public Color PriceColor
    {
        get => (Color)GetValue(PriceColorProperty);
        set => SetValue(PriceColorProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public bool ShowPriceBadge
    {
        get => (bool)GetValue(ShowPriceBadgeProperty);
        set => SetValue(ShowPriceBadgeProperty, value);
    }

    public Color PriceBadgeColor
    {
        get => (Color)GetValue(PriceBadgeColorProperty);
        set => SetValue(PriceBadgeColorProperty, value);
    }

    public ICommand ItemCommand
    {
        get => (ICommand)GetValue(ItemCommandProperty);
        set => SetValue(ItemCommandProperty, value);
    }

    public object ItemCommandParameter
    {
        get => GetValue(ItemCommandParameterProperty);
        set => SetValue(ItemCommandParameterProperty, value);
    }

    public bool ShowQuantityControls
    {
        get => (bool)GetValue(ShowQuantityControlsProperty);
        set => SetValue(ShowQuantityControlsProperty, value);
    }

    public int Quantity
    {
        get => (int)GetValue(QuantityProperty);
        set => SetValue(QuantityProperty, value);
    }

    public ICommand IncreaseCommand
    {
        get => (ICommand)GetValue(IncreaseCommandProperty);
        set => SetValue(IncreaseCommandProperty, value);
    }

    public ICommand DecreaseCommand
    {
        get => (ICommand)GetValue(DecreaseCommandProperty);
        set => SetValue(DecreaseCommandProperty, value);
    }

    public object QuantityCommandParameter
    {
        get => GetValue(QuantityCommandParameterProperty);
        set => SetValue(QuantityCommandParameterProperty, value);
    }

    private async void OnTapped(object sender, TappedEventArgs e)
    {
        if (ItemCommand is null || !ItemCommand.CanExecute(ItemCommandParameter))
        {
            return;
        }

        await Task.WhenAll(
            CardSurface.ScaleToAsync(0.98, 80, Easing.CubicOut),
            CardSurface.FadeToAsync(0.85, 80, Easing.CubicOut)
        );
        await Task.WhenAll(
            CardSurface.ScaleToAsync(1.0, 100, Easing.CubicIn),
            CardSurface.FadeToAsync(1.0, 100, Easing.CubicIn)
        );

        ItemCommand.Execute(ItemCommandParameter);
    }
}
