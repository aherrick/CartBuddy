namespace CartBuddy.Shared.Models;

public class ApiLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MethodName { get; set; } = string.Empty;
    public ApiLogDirection Direction { get; set; }
    public string Payload { get; set; } = string.Empty;

    public bool IsRawKroger => Direction is ApiLogDirection.KrogerRequest or ApiLogDirection.KrogerResponse;

    public string DirectionLabel => Direction switch
    {
        ApiLogDirection.Request => "Request",
        ApiLogDirection.Response => "Response",
        ApiLogDirection.KrogerRequest => "Kroger Request",
        ApiLogDirection.KrogerResponse => "Kroger Response",
        _ => Direction.ToString()
    };

    public string EntryTypeLabel => IsRawKroger ? "Raw Kroger" : "Mapped Payload";

    public string PayloadPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Payload))
            {
                return string.Empty;
            }

            var singleLine = Payload.ReplaceLineEndings(" ").Trim();
            if (singleLine.Length <= 88)
            {
                return singleLine;
            }

            return $"{singleLine[..88]}...";
        }
    }
}
