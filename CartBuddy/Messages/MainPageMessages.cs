using CartBuddy.Models;

namespace CartBuddy.Messages;

public sealed class ScrollToProductMessage(ProductMatch item)
{
    public ProductMatch Item { get; } = item;
}

public sealed class CloseCartRequestedMessage;

public sealed class ExpandGroupMessage(string query)
{
    public string Query { get; } = query;
}
