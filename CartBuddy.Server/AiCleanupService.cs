using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace CartBuddy.Server;

public class AiCleanupService(IConfiguration config)
{
    private readonly ChatClient _chatClient = CreateChatClient(config);

    private static ChatClient CreateChatClient(IConfiguration config)
    {
        var endpoint =
            config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
        var apiKey =
            config["AzureOpenAI:Key"]
            ?? throw new InvalidOperationException("AzureOpenAI:Key not configured");
        var deploymentName = config["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        return client.GetChatClient(deploymentName);
    }

    public async Task<List<string>> CleanupList(List<string> rawItems)
    {
        if (rawItems.Count == 0)
        {
            return [];
        }

        var itemList = string.Join("\n", rawItems);

        var completion = await _chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(
                    """
                    You are a grocery list assistant. Clean up and normalize the following grocery items into short grocery-store search terms.
                    Fix spelling, remove extra words, and use common product names that would work well as search terms on a grocery store website.
                    When it clearly improves grocery search results, add a small amount of helpful context such as a department or product form.
                    For fresh produce items, prefer a 'produce' prefix, for example 'produce jalapeno', 'produce bananas', or 'produce avocados'.
                    For deli counter meats or cheeses, prefer a 'deli' prefix, for example 'deli turkey' or 'deli American cheese'.
                    Do not add department prefixes for ordinary pantry or packaged items unless the department is truly important for search.
                    Keep each result concise and practical for store search.
                    Examples:
                    jalepenos -> produce jalapeno
                    bananas ripe -> produce bananas
                    deli turkey for sandwiches -> deli turkey
                    romaine for salad -> produce romaine lettuce
                    avacados -> produce avocados
                    Return ONLY the cleaned items, one per line, no numbering, no extra text.
                    """
                ),
                new UserChatMessage(itemList),
            ]
        );

        var result = completion.Value.Content[0].Text;
        return
        [
            .. result.Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
        ];
    }
}
