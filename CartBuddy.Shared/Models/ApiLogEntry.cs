namespace CartBuddy.Shared.Models;

public class ApiLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MethodName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // "Request" or "Response"
    public string Payload { get; set; } = string.Empty;
}
