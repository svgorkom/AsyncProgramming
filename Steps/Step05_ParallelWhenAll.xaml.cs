using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 5: RUNNING TASKS IN PARALLEL WITH Task.WhenAll
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
//
// WRONG (sequential — awaits each one before starting the next):
//     var a = await FetchA();   // start A, WAIT for A
//     var b = await FetchB();   // start B, WAIT for B (A is already done)
//
// RIGHT (parallel — starts all, then waits for all):
//     var taskA = FetchA();     // start A (no await yet! just store the Task)
//     var taskB = FetchB();     // start B immediately (A is still running)
//     await Task.WhenAll(taskA, taskB);  // now wait for both
//     var a = taskA.Result;     // or re-await: var a = await taskA;
//     var b = taskB.Result;
//
// RESULT:
// -------
// Same 3 tasks from Step 4, but now they run simultaneously.
// Total time ? longest task (2 seconds) instead of sum (4.5 seconds). ??
// ============================================================================

public partial class Step05_ParallelWhenAll : Page
{
    public Step05_ParallelWhenAll()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event handler: thin async void wrapper with try/catch.
    /// (See Step 3 for a full explanation of why we use this pattern.)
    /// </summary>
    private async void RunParallel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RunParallelDemoAsync();
        }
        catch (Exception ex)
        {
            Log($"? Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// The real work lives here in an async Task method — awaitable, testable, safe.
    /// Starts all tasks at once, then waits for all of them.
    /// </summary>
    private async Task RunParallelDemoAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        Log("? Starting PARALLEL execution...\n");

        // STEP A: Start ALL tasks at the same time.
        // Notice: no "await" yet! We're just STARTING them and storing the Task objects.
        Log("   ?? Starting all 3 tasks at the same time...");

        Task<string> profileTask = FetchUserProfileAsync();        // starts immediately
        Task<string> ordersTask = FetchOrdersAsync();              // starts immediately
        Task<string> recommendationsTask = FetchRecommendationsAsync(); // starts immediately

        // At this point, all 3 tasks are running simultaneously!
        Log("   ? All tasks are running... waiting for all to finish...");

        // STEP B: Wait for ALL tasks to complete.
        // Task.WhenAll creates a single task that finishes when every task inside is done.
        await Task.WhenAll(profileTask, ordersTask, recommendationsTask);

        // STEP C: Get the results. Since all tasks are done, .Result won't block.
        // (You can also use "await profileTask" again — it returns immediately since it's done.)
        string profile = profileTask.Result;
        string orders = ordersTask.Result;
        string recommendations = recommendationsTask.Result;

        Log($"   ? Profile: {profile}");
        Log($"   ? Orders: {orders}");
        Log($"   ? Recommendations: {recommendations}");

        stopwatch.Stop();
        Log($"\n? Total time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        Log("   (The longest task was 2.0s — compare to 4.5s sequential in Step 4!)");
        Log("   ?? We saved ~2.5 seconds by running tasks in parallel!\n");
    }

    // Same simulated operations as Step 4:

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

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
