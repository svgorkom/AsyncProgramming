using System.Diagnostics;
using System.Net.Http;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 11 VIEWMODEL: REAL HTTP CALLS WITH HttpClient
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. HttpClient -- .NET's built-in async HTTP client.
// 2. Reuse ONE HttpClient instance (avoid socket exhaustion).
// 3. All HttpClient methods accept CancellationToken.
//
// MVVM NOTE:
// ----------
// HttpClient is a service dependency. In a production app, you'd inject
// IHttpClientFactory. For this tutorial, a static instance suffices.
// ============================================================================

public partial class Step11ViewModel : StepViewModelBase
{
    // One static HttpClient instance, reused for all requests.
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    // ========================================================================
    // DEMO 1: Simple GET request to a public API.
    // ========================================================================
    [RelayCommand]
    private async Task FetchApi()
    {
        Log("--- Simple HTTP GET Request ---\n");

        try
        {
            Log("   [>] Fetching a random fact from a public API...");

            var stopwatch = Stopwatch.StartNew();

            string json = await s_httpClient.GetStringAsync(
                "https://httpbin.org/get");

            stopwatch.Stop();
            Log($"   [OK] Response received in {stopwatch.ElapsedMilliseconds}ms!");
            Log($"   [i] Response (first 300 chars):");
            Log($"   {json[..Math.Min(300, json.Length)]}");
            Log("");
        }
        catch (HttpRequestException ex)
        {
            Log($"   [ERR] HTTP Error: {ex.Message}");
            Log("   [TIP] Make sure you have an internet connection.\n");
        }
        catch (TaskCanceledException)
        {
            Log("   [TIMEOUT] Request timed out!\n");
        }
    }

    // ========================================================================
    // DEMO 2: Fetch multiple URLs in parallel (Task.WhenAll + HttpClient).
    // ========================================================================
    [RelayCommand]
    private async Task FetchParallel()
    {
        Log("--- Parallel HTTP Requests ---\n");

        string[] urls =
        [
            "https://httpbin.org/delay/1",
            "https://httpbin.org/delay/2",
            "https://httpbin.org/ip",
        ];

        try
        {
            var stopwatch = Stopwatch.StartNew();
            Log("   [>] Fetching 3 URLs in parallel...");

            Task<string>[] tasks = urls
                .Select(url => FetchUrlAsync(url))
                .ToArray();

            string[] results = await Task.WhenAll(tasks);

            stopwatch.Stop();
            Log($"\n   [OK] All 3 requests completed in {stopwatch.ElapsedMilliseconds}ms total!");
            Log("   [TIP] If sequential, it would take ~3+ seconds. Parallel is faster!\n");

            for (int i = 0; i < urls.Length; i++)
            {
                Log($"   [URL] {urls[i]}");
                Log($"      => {results[i][..Math.Min(100, results[i].Length)]}...\n");
            }
        }
        catch (Exception ex)
        {
            Log($"   [ERR] Error: {ex.Message}");
            Log("   [TIP] Make sure you have an internet connection.\n");
        }
    }

    /// <summary>
    /// Fetches a URL and returns the response as a string.
    /// </summary>
    private static async Task<string> FetchUrlAsync(string url)
    {
        HttpResponseMessage response = await s_httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
