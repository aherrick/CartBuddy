using CartBuddy.Shared.Models;
using OpenAI.Chat;
using System.Text.Json;

namespace CartBuddy.Server;

public class AiCleanupService(ChatClient chatClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<CategoryItem>> CleanupList(List<string> rawItems)
    {
        if (rawItems.Count == 0)
        {
            return [];
        }

        var itemList = string.Join("\n", rawItems);

        var completion = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(
                    """
                    You are a grocery list assistant.
                    For each input line:
                                        - normalize spelling and wording into a short store-search term only when the intended product is clear
                    - infer a category (produce, dairy, meat, seafood, bakery, frozen, pantry, beverage, household, other)
                    - do NOT prepend category words like "produce" to the cleaned term
                                        - if the input is ambiguous, brand-like, or could reasonably be a proper name, keep it essentially unchanged and use category "other"
                                        - never change a single ambiguous word into a different grocery concept just because it is lexically similar
                                        - preserve likely brands instead of translating them into generic foods
                                        - do not invent a more specific variety that the user did not ask for
                                        - if the user says a broad term like "peppers", keep it broad instead of changing it to "bell peppers"
                                        - only convert to a more specific grocery term when the original input clearly implies it

                                        Examples:
                                        - "bells" -> { "item": "bells", "category": "other" }
                                        - "bell peppers" -> { "item": "bell peppers", "category": "produce" }
                                        - "peppers" -> { "item": "peppers", "category": "produce" }
                                        - "green peppers" -> { "item": "green peppers", "category": "produce" }
                                        - "banannas" -> { "item": "bananas", "category": "produce" }
                                        - "cok" -> { "item": "coke", "category": "beverage" }
                                        - "belgioso" -> { "item": "belgioso", "category": "other" }

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
        return JsonSerializer.Deserialize<List<CategoryItem>>(result.Trim(), JsonOptions)
            ?? [.. rawItems.Select(item => new CategoryItem { Item = item, Category = "other" })];
    }
}
