namespace CartBuddy.Shared.Models;

public class CleanupRequest
{
    public List<string> Items { get; set; }
}

public class CategoryItem
{
    public string Item { get; set; }

    public string Category { get; set; }
}