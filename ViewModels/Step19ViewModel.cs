using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 19 VIEWMODEL: Task.WhenEach (.NET 9+)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Task.WhenEach -- Returns an IAsyncEnumerable<Task<T>> that yields tasks
//    in the ORDER THEY COMPLETE (not the order you started them).
//    Available in .NET 9 and later.
//
// 2. THE OLD WAY (WhenAny loop):
//    - Call Task.WhenAny to get the first completed task.
//    - Remove it from the list. Repeat until all are done.
//    - Verbose, error-prone, O(n^2) complexity.
//
// 3. THE NEW WAY (WhenEach):
//    - await foreach (var task in Task.WhenEach(tasks))
//    - Clean, readable, efficient. Each iteration gives the next completed task.
//
// 4. USE CASES:
//    - Displaying results as they arrive (fastest-first UI updates).
//    - Processing completed downloads in completion order.
//    - Logging or streaming results without waiting for all.
//
// ANALOGY:
// --------
// Imagine ordering food at 3 restaurants simultaneously.
// Task.WhenAll = wait until ALL food arrives, then eat everything at once.
// Task.WhenAny loop = keep checking "is any food here?" over and over.
// Task.WhenEach = food is handed to you in order of arrival. Just eat as it comes!
// ============================================================================

public partial class Step19ViewModel : StepViewModelBase
{
    // ========================================================================
    // DEMO 1: Process tasks in completion order using Task.WhenEach.
    // ========================================================================
    [RelayCommand]
    private async Task WhenEachDemo()
    {
        Log("--- Task.WhenEach: Process in Completion Order ---\n");

        var sw = Stopwatch.StartNew();

        // Start tasks with different durations.
        Task<string>[] tasks =
        [
            FetchDataAsync("Server-A", 2000),  // Slowest
            FetchDataAsync("Server-B", 500),   // Fastest
            FetchDataAsync("Server-C", 1200),  // Medium
        ];

        Log("   [>] All 3 tasks started simultaneously.\n");

        int order = 0;

        // Task.WhenEach yields tasks in completion order!
        await foreach (Task<string> completedTask in Task.WhenEach(tasks))
        {
            order++;
            string result = await completedTask;
            Log($"   #{order} completed: {result} ({sw.ElapsedMilliseconds}ms)");
        }

        sw.Stop();
        Log($"\n   [OK] All done in {sw.ElapsedMilliseconds}ms total.");
        Log("   [i] Results appeared in COMPLETION order, not start order!\n");
    }

    // ========================================================================
    // DEMO 2: Compare WhenAll vs WhenEach user experience.
    // ========================================================================
    [RelayCommand]
    private async Task CompareWhenAllVsWhenEach()
    {
        Log("--- WhenAll vs WhenEach: User Experience Comparison ---\n");

        // --- WhenAll: user sees nothing until ALL are done ---
        Log("[>] Task.WhenAll -- wait for all, then display:");
        var sw = Stopwatch.StartNew();

        Task<string>[] allTasks =
        [
            FetchDataAsync("API-1", 1500),
            FetchDataAsync("API-2", 300),
            FetchDataAsync("API-3", 800),
        ];

        string[] allResults = await Task.WhenAll(allTasks);
        Log($"   [i] After {sw.ElapsedMilliseconds}ms -- ALL results at once:");
        foreach (string r in allResults) Log($"   - {r}");

        Log("");

        // --- WhenEach: user sees results immediately ---
        Log("[OK] Task.WhenEach -- display each as it arrives:");
        sw.Restart();

        Task<string>[] eachTasks =
        [
            FetchDataAsync("API-1", 1500),
            FetchDataAsync("API-2", 300),
            FetchDataAsync("API-3", 800),
        ];

        await foreach (Task<string> done in Task.WhenEach(eachTasks))
        {
            string result = await done;
            Log($"   [i] At {sw.ElapsedMilliseconds}ms -- {result}");
        }

        sw.Stop();
        Log("\n   [i] WhenEach showed the first result after ~300ms.");
        Log("   [i] WhenAll showed nothing until ~1500ms. Better UX with WhenEach!\n");
    }

    // ========================================================================
    // DEMO 3: WhenEach with error handling.
    // ========================================================================
    [RelayCommand]
    private async Task WhenEachWithErrors()
    {
        Log("--- Task.WhenEach with Error Handling ---\n");

        Task<string>[] tasks =
        [
            FetchDataAsync("Good-Server-1", 400),
            FailingFetchAsync("Bad-Server", 700),
            FetchDataAsync("Good-Server-2", 1000),
        ];

        Log("   [>] Started 3 tasks (one will fail)...\n");

        int order = 0;
        await foreach (Task<string> completed in Task.WhenEach(tasks))
        {
            order++;
            try
            {
                string result = await completed;
                Log($"   [OK] #{order}: {result}");
            }
            catch (Exception ex)
            {
                Log($"   [ERR] #{order}: FAILED -- {ex.Message}");
            }
        }

        Log("\n   [i] Unlike WhenAll (which throws on first error), WhenEach lets");
        Log("   [i] you handle each task's success/failure individually.\n");
    }

    // --- Helper methods ---

    private static async Task<string> FetchDataAsync(string server, int delayMs)
    {
        await Task.Delay(delayMs);
        return $"{server} responded in {delayMs}ms";
    }

    private static async Task<string> FailingFetchAsync(string server, int delayMs)
    {
        await Task.Delay(delayMs);
        throw new InvalidOperationException($"{server}: Connection refused!");
    }
}
