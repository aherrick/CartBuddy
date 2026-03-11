using CartBuddy.Shared.Models;
using System.Collections.ObjectModel;
using System.Text;

namespace CartBuddy.Models;

public class LogTransactionGroup(Guid transactionId, IEnumerable<ApiLogEntry> entries)
    : ObservableCollection<ApiLogEntry>(entries.OrderBy(entry => entry.Timestamp))
{
    public Guid TransactionId { get; } = transactionId;

    public ApiLogEntry LatestEntry => this[^1];

    public string MethodName => LatestEntry.MethodName;

    public DateTime Timestamp => LatestEntry.Timestamp;

    public string EntryCountLabel => Count == 1 ? "1 entry" : $"{Count} entries";

    public string DirectionSummary => string.Join(" • ", this.Select(entry => entry.DirectionLabel));

    public string PayloadPreview => LatestEntry.PayloadPreview;

    public string CopyText()
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Transaction: {TransactionId}");
        builder.AppendLine($"Method: {MethodName}");
        builder.AppendLine();

        for (var i = 0; i < Count; i++)
        {
            var entry = this[i];
            builder.AppendLine($"=== {entry.DirectionLabel} | {entry.Timestamp:yyyy-MM-dd HH:mm:ss} UTC ===");
            builder.AppendLine(entry.Payload);

            if (i < Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}