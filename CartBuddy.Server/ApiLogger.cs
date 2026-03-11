using CartBuddy.Shared.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CartBuddy.Server;

public class ApiLogger
{
    private const int MaxEntries = 200;
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };
    private readonly ConcurrentQueue<ApiLogEntry> _entries = new();

    public void Log(
        string methodName,
        ApiLogDirection direction,
        object payload,
        Guid transactionId = default
    )
    {
        var formattedPayload = payload is string rawPayload
            ? rawPayload
            : JsonSerializer.Serialize(payload, IndentedOptions);

        _entries.Enqueue(new ApiLogEntry
        {
            TransactionId = transactionId,
            MethodName = methodName,
            Direction = direction,
            Payload = formattedPayload,
        });

        while (_entries.Count > MaxEntries)
        {
            _entries.TryDequeue(out _);
        }
    }

    public List<ApiLogEntry> GetAll() =>
        [.. _entries.OrderByDescending(e => e.Timestamp)];
}
