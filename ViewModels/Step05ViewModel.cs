using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 5 VIEWMODEL: RUNNING TASKS IN PARALLEL WITH Task.WhenAll
// ============================================================================
//
// KEY CONCEPT:
// ------------
// Task.WhenAll(task1, task2, task3) takes multiple tasks and returns a NEW task
// that completes when ALL of them are done.
//
// THE TRICK:
// ----------
// To run tasks in parallel, you must:
//   1. START all tasks WITHOUT awaiting them (store the Task in a variable).
//   2. THEN await them all at once using Task.WhenAll.
// ============================================================================

public partial class Step05ViewModel : StepViewModelBase
{
    /// <summary>
    /// Starts all tasks at once, then waits for all of them.
    /// </summary>
    [RelayCommand]
    private async Task RunParallel()
    {
        var stopwatch = Stopwatch.StartNew();

        Log("[>] Starting PARALLEL execution...\n");

        // Start ALL tasks at the same time -- no "await" yet!
        Log("   [>] Starting all 3 tasks at the same time...");

        Task<string> profileTask = FetchUserProfileAsync();
        Task<string> ordersTask = FetchOrdersAsync();
        Task<string> recommendationsTask = FetchRecommendationsAsync();

        Log("   [i] All tasks are running... waiting for all to finish...");

        // Wait for ALL tasks to complete.
        await Task.WhenAll(profileTask, ordersTask, recommendationsTask);

        // Get results -- all tasks are done, so .Result won't block.
        string profile = profileTask.Result;
        string orders = ordersTask.Result;
        string recommendations = recommendationsTask.Result;

        Log($"   [OK] Profile: {profile}");
        Log($"   [OK] Orders: {orders}");
        Log($"   [OK] Recommendations: {recommendations}");

        stopwatch.Stop();
        Log($"\n[DONE] Total time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        Log("   (The longest task was 2.0s -- compare to 4.5s sequential in Step 4!)");
        Log("   [TIP] We saved ~2.5 seconds by running tasks in parallel!\n");
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
