using System.Globalization;
using CartBuddy.Models;
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
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string direction && direction == "Request"
            ? Color.FromArgb("#5B8DEF")
            : Color.FromArgb("#34C759");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class DirectionGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string direction && direction == "Request" ? "\uF062" : "\uF063"; // FA arrow-up / arrow-down

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
