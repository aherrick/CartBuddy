using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace CartBuddy.Services;

public class AiCleanupService(IConfiguration config)
{
    public async Task<List<string>> CleanupList(List<string> rawItems)
    {
        if (rawItems.Count == 0)
        {
            return [];
        }

        var client = new AzureOpenAIClient(
            new Uri(
                config["AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured")
            ),
            new AzureKeyCredential(
                config["AzureOpenAI:Key"]
                    ?? throw new InvalidOperationException("AzureOpenAI:Key not configured")
            )
        );

        var chatClient = client.GetChatClient(Constants.AzureOpenAIDeployment);
        var itemList = string.Join("\n", rawItems);

        var completion = await chatClient.CompleteChatAsync(
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