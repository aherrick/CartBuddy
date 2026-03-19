using System.Windows.Input;

namespace CartBuddy.Controls;

public partial class CartItemView : BaseItemView
{
    public static readonly BindableProperty ShowQuantityControlsProperty =
        BindableProperty.Create(nameof(ShowQuantityControls), typeof(bool), typeof(CartItemView), false);

    public static readonly BindableProperty QuantityProperty =
        BindableProperty.Create(nameof(Quantity), typeof(int), typeof(CartItemView), 0);

    public static readonly BindableProperty IncreaseCommandProperty =
        BindableProperty.Create(nameof(IncreaseCommand), typeof(ICommand), typeof(CartItemView));

    public static readonly BindableProperty DecreaseCommandProperty =
        BindableProperty.Create(nameof(DecreaseCommand), typeof(ICommand), typeof(CartItemView));

    public static readonly BindableProperty QuantityCommandParameterProperty =
        BindableProperty.Create(nameof(QuantityCommandParameter), typeof(object), typeof(CartItemView));

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

    public CartItemView()
    {
        InitializeComponent();
    }
}