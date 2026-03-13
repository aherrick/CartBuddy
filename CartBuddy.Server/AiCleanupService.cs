using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace CartBuddy.Server;

public class AiCleanupService
{
    private readonly ChatClient _chatClient;

    public AiCleanupService(IConfiguration config)
    {
        var endpoint =
            config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
        var apiKey =
            config["AzureOpenAI:Key"]
            ?? throw new InvalidOperationException("AzureOpenAI:Key not configured");
        var deploymentName = config["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = client.GetChatClient(deploymentName);
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
                    "You are a grocery list assistant. Clean up and standardize the following grocery items. "
                        + "Fix spelling, use common product names that would work well as search terms on a grocery store website. "
                        + "Return ONLY the cleaned items, one per line, no numbering, no extra text."
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
