using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 9 VIEWMODEL: Task.WhenAny -- RESPONDING TO THE FIRST COMPLETED TASK
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Task.WhenAny -- returns a Task<Task> that completes when the FIRST finishes.
// 2. Timeout Pattern -- race between operation and Task.Delay.
// ============================================================================

public partial class Step09ViewModel : StepViewModelBase
{
    // ========================================================================
    // SCENARIO 1: Racing -- Query 3 servers, use the first to respond.
    // ========================================================================
    [RelayCommand]
    private async Task Race()
    {
        Log("--- Racing 3 Servers ---\n");

        Task<string> server1 = QueryServerAsync("Server-A", 1500);
        Task<string> server2 = QueryServerAsync("Server-B", 800);
        Task<string> server3 = QueryServerAsync("Server-C", 2000);

        Log("   [>] All 3 servers queried simultaneously...");

        Task<string> winner = await Task.WhenAny(server1, server2, server3);
        string result = await winner;
        Log($"   [1st] Winner: {result}");
        Log("   [i] (Other servers are still running in background)\n");
    }

    // ========================================================================
    // SCENARIO 2: Timeout -- Give an operation a maximum time to complete.
    // ========================================================================
    [RelayCommand]
    private async Task Timeout()
    {
        Log("--- Timeout Pattern ---\n");

        Task<string> operation = SlowOperationAsync();
        Task timeout = Task.Delay(2000);

        Log("   [>] Starting operation with 2-second timeout...");

        Task completedFirst = await Task.WhenAny(operation, timeout);

        if (completedFirst == operation)
        {
            string result = await operation;
            Log($"   [OK] Operation completed in time: {result}");
        }
        else
        {
            Log("   [TIMEOUT] The operation took too long.");
            Log("   [i] The operation is still running, but we're moving on.");
            Log("   [TIP] In production, you'd cancel it with a CancellationToken.");
        }
        Log("");
    }

    // --- Helper methods ---

    private static async Task<string> QueryServerAsync(string serverName, int delayMs)
    {
        await Task.Delay(delayMs);
        return $"{serverName} responded in {delayMs}ms";
    }

    private static async Task<string> SlowOperationAsync()
    {
        await Task.Delay(3000);
        return "Data from slow operation";
    }
}
