using System.Globalization;
using CartBuddy.Models;
using CartBuddy.Shared.Models;
using CartBuddy.ViewModels;

namespace CartBuddy.Converters;

public class ExpandIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool expanded && expanded ? "\uF078" : "\uF054";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class ExpandAllTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool expanded && expanded ? "Collapse All" : "Expand All";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class GroupInfoConverter : IValueConverter
{
    public MainViewModel ViewModel { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string query || ViewModel is null)
        {
            return GetDefault(parameter as string);
        }

        var group = ViewModel.SearchGroups.FirstOrDefault(g => g.Query == query);
        if (group is null)
        {
            return GetDefault(parameter as string);
        }

        return parameter switch
        {
            "IsCompleted" => group.IsCompleted,
            "HasMore" => group.HasMore,
            "PageSummary" => group.PageSummary,
            "Group" => group,
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static object GetDefault(string param) =>
        param switch
        {
            "IsCompleted" => false,
            "HasMore" => false,
            "PageSummary" => string.Empty,
            _ => null
        };
}

public class DirectionColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ApiLogDirection direction)
        {
            return Color.FromArgb("#34C759");
        }

        return direction switch
        {
            ApiLogDirection.Request => Color.FromArgb("#5B8DEF"),
            ApiLogDirection.Response => Color.FromArgb("#34C759"),
            ApiLogDirection.KrogerRequest => Color.FromArgb("#2F6FED"),
            ApiLogDirection.KrogerResponse => Color.FromArgb("#14B8A6"),
            _ => Color.FromArgb("#7C7C88")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class DirectionGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ApiLogDirection direction)
        {
            return "\uF15C";
        }

        return direction switch
        {
            ApiLogDirection.Request => "\uF062",
            ApiLogDirection.Response => "\uF063",
            ApiLogDirection.KrogerRequest => "\uF0C1",
            ApiLogDirection.KrogerResponse => "\uF15C",
            _ => "\uF059"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
