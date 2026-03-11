using CartBuddy.Shared.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CartBuddy.Server;

public class ApiLogger
{
    private const int MaxEntries = 200;
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };
    private readonly ConcurrentQueue<ApiLogEntry> _entries = new();

    public void Log(string methodName, string direction, object payload)
    {
        var json = JsonSerializer.Serialize(payload, IndentedOptions);
        _entries.Enqueue(new ApiLogEntry
        {
            MethodName = methodName,
            Direction = direction,
            Payload = json,
        });

        while (_entries.Count > MaxEntries)
        {
            _entries.TryDequeue(out _);
        }
    }

    public List<ApiLogEntry> GetAll() =>
        [.. _entries.OrderByDescending(e => e.Timestamp)];
}
