using CartBuddy.Shared.Models;

namespace CartBuddy.Models;

internal static class CategoryItemExtensions
{
    public static CategoryItem Clone(this CategoryItem item) =>
        new()
        {
            Item = item.Item.Trim(),
            Category = string.IsNullOrWhiteSpace(item.Category) ? "other" : item.Category.Trim(),
        };
}
