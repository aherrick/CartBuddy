namespace CartBuddy.Shared.Models;

public class CleanupRequest
{
    public List<string> Items { get; set; }
}

public class CleanupResponse
{
    public List<string> CleanedItems { get; set; }
}