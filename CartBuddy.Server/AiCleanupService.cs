using Azure;
using Azure.AI.OpenAI;
using CartBuddy.Shared.Models;
using OpenAI.Chat;
using System.Text.Json;

namespace CartBuddy.Server;

public class AiCleanupService(IConfiguration config)
{
    private readonly ChatClient _chatClient = CreateChatClient(config);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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

    public async Task<List<CategoryItem>> CleanupList(List<string> rawItems)
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
                    You are a grocery list assistant.
                    For each input line:
                    - normalize spelling and wording into a short store-search term
                    - infer a category (produce, dairy, meat, seafood, bakery, frozen, pantry, beverage, household, other)
                    - do NOT prepend category words like "produce" to the cleaned term

                    Return ONLY valid JSON as an array of objects with this exact schema:
                    [
                      { "item": "jalapeno", "category": "produce" }
                    ]

                    Keep category lowercase. Keep one output object per input item in the same order.
                    """
                ),
                new UserChatMessage(itemList),
            ]
        );

        var result = completion.Value.Content[0].Text;
        return JsonSerializer.Deserialize<List<CategoryItem>>(result.Trim(), JsonOptions);
    }
}
