using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 4: SEQUENTIAL ASYNC CALLS
// ============================================================================
//
// KEY CONCEPT:
// ------------
// When you write:
//     var a = await DoThingA();
//     var b = await DoThingB();
//     var c = await DoThingC();
//
// Each "await" WAITS for the previous one to complete before starting the next.
// This is SEQUENTIAL execution — tasks run one after another, like a queue.
//
// WHEN TO USE SEQUENTIAL:
// -----------------------
// Use sequential when each task DEPENDS on the result of the previous one.
// Example: 
//     var userId = await GetUserIdAsync("Alice");     // Step 1: need the ID first
//     var orders = await GetOrdersAsync(userId);       // Step 2: uses the ID from step 1
//     var total  = await CalculateTotalAsync(orders);  // Step 3: uses orders from step 2
//
// DOWNSIDE:
// ---------
// If the tasks are INDEPENDENT (don't depend on each other), running them 
// sequentially wastes time. We'll fix this in Step 5 with Task.WhenAll!
// ============================================================================

public partial class Step04_SequentialCalls : Page
{
    public Step04_SequentialCalls()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event handler: thin async void wrapper with try/catch.
    /// (See Step 3 for a full explanation of why we use this pattern.)
    /// </summary>
    private async void RunSequential_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RunSequentialDemoAsync();
        }
        catch (Exception ex)
        {
            Log($"? Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// The real work lives here in an async Task method — awaitable, testable, safe.
    /// Runs three tasks one after another. Total time = sum of all delays.
    /// </summary>
    private async Task RunSequentialDemoAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        Log("? Starting SEQUENTIAL execution...\n");

        // --- Task 1: Simulate fetching user profile (takes 1.5 seconds) ---
        Log("   ?? Fetching user profile...");
        string profile = await FetchUserProfileAsync();
        Log($"   ? Got profile: {profile}");

        // --- Task 2: Simulate fetching orders (takes 2 seconds) ---
        // This only starts AFTER the profile is fetched.
        Log("   ?? Fetching recent orders...");
        string orders = await FetchOrdersAsync();
        Log($"   ? Got orders: {orders}");

        // --- Task 3: Simulate fetching recommendations (takes 1 second) ---
        // This only starts AFTER orders are fetched.
        Log("   ?? Fetching recommendations...");
        string recommendations = await FetchRecommendationsAsync();
        Log($"   ? Got recommendations: {recommendations}");

        stopwatch.Stop();
        Log($"\n? Total time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        Log("   (1.5 + 2.0 + 1.0 = 4.5 seconds — each waited for the previous one)");
        Log("   ?? In Step 5, we'll run these in PARALLEL and save time!\n");
    }

    // Simulated async operations with different durations:

    private static async Task<string> FetchUserProfileAsync()
    {
        await Task.Delay(1500); // Simulate 1.5 seconds of work
        return "Alice (Premium Member)";
    }

    private static async Task<string> FetchOrdersAsync()
    {
        await Task.Delay(2000); // Simulate 2 seconds of work
        return "Order #101, Order #102, Order #103";
    }

    private static async Task<string> FetchRecommendationsAsync()
    {
        await Task.Delay(1000); // Simulate 1 second of work
        return "Widget Pro, Gadget Plus, ThingaMajig";
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
