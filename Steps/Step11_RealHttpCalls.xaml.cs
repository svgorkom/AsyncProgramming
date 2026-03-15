using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 11: REAL HTTP CALLS WITH HttpClient
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. HttpClient — .NET's built-in HTTP client. All methods are async.
//    - GetStringAsync(url)        ? downloads a URL and returns the content as a string.
//    - GetAsync(url)              ? returns an HttpResponseMessage with status code, headers, etc.
//    - PostAsync(url, content)    ? sends data to a URL.
//    - GetStreamAsync(url)        ? returns a stream for large downloads.
//
// 2. ?? REUSE HttpClient!
//    - Creating a new HttpClient for every request is BAD.
//    - It can cause socket exhaustion (running out of network connections).
//    - Best practice: create ONE HttpClient and reuse it for all requests.
//    - In real apps, use IHttpClientFactory (from dependency injection).
//
// 3. HttpClient + CancellationToken
//    - All HttpClient methods accept an optional CancellationToken.
//    - Example: await client.GetStringAsync(url, cancellationToken);
//    - This lets users cancel slow downloads.
//
// 4. Why is HttpClient async?
//    - Network requests involve WAITING (for the server to respond).
//    - During that wait, async frees up the UI thread — no freezing!
//    - This is called "I/O-bound" work — the CPU isn't busy, it's just waiting.
//    - Perfect use case for async/await (no Task.Run needed!).
// ============================================================================

public partial class Step11_RealHttpCalls : Page
{
    // ? One static HttpClient instance, reused for all requests.
    // Never create a new HttpClient per request!
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public Step11_RealHttpCalls()
    {
        InitializeComponent();
    }

    // ========================================================================
    // DEMO 1: Simple GET request to a public API.
    // ========================================================================
    private async void FetchApi_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Simple HTTP GET Request ---\n");

        try
        {
            Log("   ? Fetching a random fact from a public API...");

            var stopwatch = Stopwatch.StartNew();

            // GetStringAsync is the simplest way to download text from a URL.
            // It's fully async — the UI stays responsive while waiting for the server.
            string json = await s_httpClient.GetStringAsync(
                "https://httpbin.org/get");

            stopwatch.Stop();
            Log($"   ? Response received in {stopwatch.ElapsedMilliseconds}ms!");
            Log($"   ?? Response (first 300 chars):");
            Log($"   {json[..Math.Min(300, json.Length)]}");
            Log("");
        }
        catch (HttpRequestException ex)
        {
            // Network errors: no internet, DNS failure, server error, etc.
            Log($"   ?? HTTP Error: {ex.Message}");
            Log("   ?? Make sure you have an internet connection.\n");
        }
        catch (TaskCanceledException)
        {
            // This happens when the request times out.
            Log("   ?? Request timed out!\n");
        }
    }

    // ========================================================================
    // DEMO 2: Fetch multiple URLs in parallel (Task.WhenAll + HttpClient).
    // ========================================================================
    private async void FetchParallel_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Parallel HTTP Requests ---\n");

        // Three URLs to fetch simultaneously.
        string[] urls =
        [
            "https://httpbin.org/delay/1",   // delays 1 second
            "https://httpbin.org/delay/2",   // delays 2 seconds
            "https://httpbin.org/ip",        // instant response
        ];

        try
        {
            var stopwatch = Stopwatch.StartNew();
            Log("   ?? Fetching 3 URLs in parallel...");

            // Start ALL requests at the same time (no await yet!).
            Task<string>[] tasks = urls
                .Select(url => FetchUrlAsync(url))
                .ToArray();

            // Wait for ALL of them to complete.
            string[] results = await Task.WhenAll(tasks);

            stopwatch.Stop();
            Log($"\n   ? All 3 requests completed in {stopwatch.ElapsedMilliseconds}ms total!");
            Log("   ?? If sequential, it would take ~3+ seconds. Parallel is faster!\n");

            for (int i = 0; i < urls.Length; i++)
            {
                Log($"   ?? {urls[i]}");
                Log($"      ? {results[i][..Math.Min(100, results[i].Length)]}...\n");
            }
        }
        catch (Exception ex)
        {
            Log($"   ?? Error: {ex.Message}");
            Log("   ?? Make sure you have an internet connection.\n");
        }
    }

    /// <summary>
    /// Fetches a URL and returns the response as a string.
    /// This is a reusable async method — notice it doesn't touch any UI controls.
    /// </summary>
    private static async Task<string> FetchUrlAsync(string url)
    {
        HttpResponseMessage response = await s_httpClient.GetAsync(url);

        // EnsureSuccessStatusCode throws if the status code is not 2xx (success).
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
