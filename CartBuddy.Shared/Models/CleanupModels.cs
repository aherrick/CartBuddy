namespace CartBuddy.Shared.Models;

public class CleanupRequest
{
    public List<string> Items { get; set; }
}

public class CleanupResponse
{
    public List<string> CleanedItems { get; set; }

    public List<CleanupItem> ClassifiedItems { get; set; }
}

public class CleanupItem
{
    public string Item { get; set; }

    public string Category { get; set; }
}