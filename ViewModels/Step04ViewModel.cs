using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 4 VIEWMODEL: SEQUENTIAL ASYNC CALLS
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
// ============================================================================

public partial class Step04ViewModel : StepViewModelBase
{
    /// <summary>
    /// Runs three tasks one after another. Total time = sum of all delays.
    /// </summary>
    [RelayCommand]
    private async Task RunSequential()
    {
        var stopwatch = Stopwatch.StartNew();

        Log("?? Starting SEQUENTIAL execution...\n");

        Log("   ?? Fetching user profile...");
        string profile = await FetchUserProfileAsync();
        Log($"   ? Got profile: {profile}");

        Log("   ?? Fetching recent orders...");
        string orders = await FetchOrdersAsync();
        Log($"   ? Got orders: {orders}");

        Log("   ?? Fetching recommendations...");
        string recommendations = await FetchRecommendationsAsync();
        Log($"   ? Got recommendations: {recommendations}");

        stopwatch.Stop();
        Log($"\n? Total time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        Log("   (1.5 + 2.0 + 1.0 = 4.5 seconds — each waited for the previous one)");
        Log("   ?? In Step 5, we'll run these in PARALLEL and save time!\n");
    }

    private static async Task<string> FetchUserProfileAsync()
    {
        await Task.Delay(1500);
        return "Alice (Premium Member)";
    }

    private static async Task<string> FetchOrdersAsync()
    {
        await Task.Delay(2000);
        return "Order #101, Order #102, Order #103";
    }

    private static async Task<string> FetchRecommendationsAsync()
    {
        await Task.Delay(1000);
        return "Widget Pro, Gadget Plus, ThingaMajig";
    }
}
