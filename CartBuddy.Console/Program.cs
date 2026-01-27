using System.Diagnostics;

Console.WriteLine("Starting Rate Limit Test against localhost...");
var client = new HttpClient();

// Assuming port 7270 from launchSettings.json - Update if running elsewhere
var url = "https://localhost:7270/api/health";

int successCount = 0;
int blockedCount = 0;
var sw = Stopwatch.StartNew();

// Try to hit endpoints 110 times (Limit is 100/min)
Console.WriteLine("Sending 110 requests...");
for (int i = 0; i < 110; i++)
{
    try
    {
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            successCount++;
            Console.Write(".");
        }
        else if ((int)response.StatusCode == 429)
        {
            blockedCount++;
            Console.Write("X"); // X marks the spot (blocked)
        }
        else
        {
            Console.Write("?");
            Console.WriteLine($"\nUnexpected: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nRequest Error: {ex.Message}");
        // If connection refused, server might not be running
        if (ex.Message.Contains("connection refused"))
        {
            Console.WriteLine(
                "Make sure CartBuddy.Server is running! (dotnet run --project CartBuddy.Server)"
            );
            return;
        }
    }
}

sw.Stop();
Console.WriteLine();
Console.WriteLine($"Test completed in {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"Successful requests: {successCount}");
Console.WriteLine($"Blocked (429) requests: {blockedCount}");

if (blockedCount > 0)
    Console.WriteLine("✅ Rate limiting is working! Requests were blocked.");
else
    Console.WriteLine("❌ Rate limiting did not trigger. (Limit is 100/min)");

Console.ReadLine();
