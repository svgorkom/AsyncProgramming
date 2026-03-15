using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 9: Task.WhenAny — RESPONDING TO THE FIRST COMPLETED TASK
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Task.WhenAny(task1, task2, task3)
//    - Returns a Task<Task> — a task that completes when the FIRST inner task finishes.
//    - The returned value is the task that won the race.
//    - The OTHER tasks keep running! WhenAny doesn't cancel them. If you want to
//      cancel the losers, use CancellationTokens (Step 6).
//
// 2. Timeout Pattern:
//    - Start your real operation AND a Task.Delay (the timeout).
//    - WhenAny between them: if Task.Delay finishes first ? timeout!
//    - This is a clean way to add timeouts without blocking.
//
// 3. Process-as-available Pattern:
//    - Start many tasks, put them in a list.
//    - In a loop: await WhenAny to get the first finished task.
//    - Remove that task from the list, process it, repeat.
//    - This processes results as fast as possible.
// ============================================================================

public partial class Step09_WhenAny : Page
{
    public Step09_WhenAny()
    {
        InitializeComponent();
    }

    // ========================================================================
    // SCENARIO 1: Racing — Query 3 servers, use the first to respond.
    // ========================================================================
    private async void Race_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Racing 3 Servers ---\n");

        // Start all three "server queries" at the same time.
        // Each takes a different (random-ish) amount of time.
        Task<string> server1 = QueryServerAsync("Server-A", 1500);
        Task<string> server2 = QueryServerAsync("Server-B", 800);
        Task<string> server3 = QueryServerAsync("Server-C", 2000);

        Log("   ?? All 3 servers queried simultaneously...");

        // WhenAny returns whichever task finishes FIRST.
        // The return type is Task<Task<string>> — we await it to get the winning Task<string>.
        Task<string> winner = await Task.WhenAny(server1, server2, server3);

        // Now "winner" is the task that finished first. Await it to get the result.
        string result = await winner;
        Log($"   ?? Winner: {result}");

        // Note: the other servers are STILL running in the background!
        // If you want to stop them, you'd need CancellationTokens (see Step 6).
        Log("   ?? (Other servers are still running in background)\n");
    }

    // ========================================================================
    // SCENARIO 2: Timeout — Give an operation a maximum time to complete.
    // ========================================================================
    private async void Timeout_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Timeout Pattern ---\n");

        // Our real operation (simulated: takes 3 seconds)
        Task<string> operation = SlowOperationAsync();

        // Our timeout (2 seconds)
        Task timeout = Task.Delay(2000);

        Log("   ? Starting operation with 2-second timeout...");

        // Race between the operation and the timeout.
        Task completedFirst = await Task.WhenAny(operation, timeout);

        if (completedFirst == operation)
        {
            // The operation finished before the timeout — success!
            string result = await operation;
            Log($"   ? Operation completed in time: {result}");
        }
        else
        {
            // The timeout finished first — the operation is too slow!
            Log("   ?? TIMEOUT! The operation took too long.");
            Log("   ?? The operation is still running, but we're moving on.");
            Log("   ?? In production, you'd cancel it with a CancellationToken.");
        }
        Log("");
    }

    // --- Helper methods ---

    /// <summary>
    /// Simulates querying a server. Each server has a different response time.
    /// </summary>
    private static async Task<string> QueryServerAsync(string serverName, int delayMs)
    {
        await Task.Delay(delayMs);
        return $"{serverName} responded in {delayMs}ms";
    }

    /// <summary>
    /// Simulates an operation that takes 3 seconds (longer than our 2-second timeout).
    /// </summary>
    private static async Task<string> SlowOperationAsync()
    {
        await Task.Delay(3000);
        return "Data from slow operation";
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
