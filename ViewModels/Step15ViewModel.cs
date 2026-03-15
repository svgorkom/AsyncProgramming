using System.Diagnostics;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 15 VIEWMODEL: Task.Run & CPU-BOUND vs I/O-BOUND WORK
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. I/O-BOUND work: Waiting for something external (network, disk, database).
//    - The thread is IDLE while waiting. No CPU usage.
//    - Use "await" directly: await httpClient.GetAsync(url);
//    - Do NOT wrap in Task.Run — that wastes a thread pool thread to just wait.
//
// 2. CPU-BOUND work: Crunching numbers, processing data, image manipulation.
//    - The thread is BUSY doing calculations. 100% CPU on that thread.
//    - Use Task.Run to move it OFF the UI thread:
//      var result = await Task.Run(() => HeavyCalculation());
//
// 3. THE RULE:
//    - I/O-bound ? await the async API directly.
//    - CPU-bound ? wrap in Task.Run to keep UI responsive.
//
// 4. DON'T USE Task.Run IN LIBRARY CODE:
//    - Libraries should expose naturally async APIs.
//    - Let the CALLER decide whether to use Task.Run.
//    - Only use Task.Run at the "edge" (UI layer, event handlers).
//
// ANALOGY:
// --------
// I/O-bound = ordering food at a restaurant. You wait, but you're not cooking.
// CPU-bound = cooking the food yourself. You're busy the whole time.
// Task.Run = hiring a cook (background thread) so you (UI thread) stay free.
// ============================================================================

public partial class Step15ViewModel : StepViewModelBase
{
    private readonly Dispatcher _dispatcher;

    public Step15ViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    // ========================================================================
    // DEMO 1: CPU-bound work WITHOUT Task.Run (blocks UI).
    // ========================================================================
    [RelayCommand]
    private void CpuBoundBlocking()
    {
        Log("--- CPU-Bound WITHOUT Task.Run (BAD) ---\n");
        Log("   ?? Running heavy calculation on UI thread...");
        Log("   ?? Try to interact with the UI — it's FROZEN!\n");

        var sw = Stopwatch.StartNew();

        // ? This blocks the UI thread!
        long result = HeavyCalculation();

        sw.Stop();
        Log($"   ? Result: {result:N0}");
        Log($"   ?? Took: {sw.ElapsedMilliseconds}ms");
        Log("   ? The UI was frozen the entire time!\n");
    }

    // ========================================================================
    // DEMO 2: CPU-bound work WITH Task.Run (UI stays responsive).
    // ========================================================================
    [RelayCommand]
    private async Task CpuBoundWithTaskRun()
    {
        Log("--- CPU-Bound WITH Task.Run (GOOD) ---\n");
        Log("   ? Running heavy calculation on background thread...");
        Log("   ? The UI stays responsive! Try clicking around.\n");

        var sw = Stopwatch.StartNew();

        // ? Task.Run moves the work to a background thread.
        long result = await Task.Run(() => HeavyCalculation());

        sw.Stop();
        Log($"   ? Result: {result:N0}");
        Log($"   ?? Took: {sw.ElapsedMilliseconds}ms");
        Log("   ?? UI was responsive the entire time!\n");
    }

    // ========================================================================
    // DEMO 3: I/O-bound work — DON'T wrap in Task.Run.
    // ========================================================================
    [RelayCommand]
    private async Task IoBoundCorrect()
    {
        Log("--- I/O-Bound: Just Use await (no Task.Run needed) ---\n");

        Log("   ? Simulating 3 I/O operations (network calls)...\n");

        // ? CORRECT: Just await the async I/O method directly.
        Log("   ?? Calling API 1...");
        string r1 = await SimulateNetworkCallAsync("API-1", 800);
        Log($"   ? {r1}");

        Log("   ?? Calling API 2...");
        string r2 = await SimulateNetworkCallAsync("API-2", 600);
        Log($"   ? {r2}");

        Log("   ?? Calling API 3...");
        string r3 = await SimulateNetworkCallAsync("API-3", 400);
        Log($"   ? {r3}");

        Log("\n   ?? No Task.Run needed! The thread was already free during await.");
        Log("   ?? Wrapping I/O in Task.Run would waste a thread pool thread.\n");
    }

    // ========================================================================
    // DEMO 4: Mixed — CPU + I/O together.
    // ========================================================================
    [RelayCommand]
    private async Task MixedWorkload()
    {
        Log("--- Mixed: CPU + I/O Together ---\n");

        // Step 1: I/O-bound — await directly.
        Log("   ?? Step 1: Fetching data (I/O-bound — await directly)...");
        string data = await SimulateNetworkCallAsync("DataService", 500);
        Log($"   ? Got: {data}");

        // Step 2: CPU-bound — use Task.Run.
        Log("   ?? Step 2: Processing data (CPU-bound — Task.Run)...");
        long processed = await Task.Run(() => HeavyCalculation());
        Log($"   ? Processed: {processed:N0}");

        // Step 3: I/O-bound — await directly again.
        Log("   ?? Step 3: Saving results (I/O-bound — await directly)...");
        await SimulateNetworkCallAsync("SaveService", 300);
        Log("   ? Saved!");

        Log("\n   ?? Summary:");
        Log("   • I/O work ? await directly (no Task.Run)");
        Log("   • CPU work ? await Task.Run(() => ...)");
        Log("   • UI stayed responsive throughout!\n");
    }

    // --- Helper methods ---

    /// <summary>
    /// Simulates CPU-heavy work — burns CPU cycles calculating a sum.
    /// </summary>
    private static long HeavyCalculation()
    {
        long sum = 0;
        for (int i = 0; i < 200_000_000; i++)
        {
            sum += i;
        }
        return sum;
    }

    /// <summary>
    /// Simulates an I/O-bound async operation (network call, disk read, etc.)
    /// </summary>
    private static async Task<string> SimulateNetworkCallAsync(string name, int delayMs)
    {
        await Task.Delay(delayMs);
        return $"{name} responded in {delayMs}ms";
    }
}
