using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace CartBuddy.Converters;

public class SyncfusionItemTappedEventArgsConverter : BaseConverterOneWay<Syncfusion.Maui.ListView.ItemTappedEventArgs, object>
{
    public override object DefaultConvertReturnValue { get; set; } = null;

    public override object ConvertFrom(Syncfusion.Maui.ListView.ItemTappedEventArgs value, CultureInfo culture)
    {
        return value?.DataItem;
    }
}