using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 14 VIEWMODEL: DEADLOCKS & .Result/.Wait() ANTI-PATTERNS
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. DEADLOCK: When two things wait for each other forever.
//    - In WPF, calling .Result or .Wait() on the UI thread BLOCKS the UI thread.
//    - The awaited task needs the UI thread to resume (because of SynchronizationContext).
//    - UI thread is blocked ? task can't resume ? task never completes ? deadlock!
//
// 2. .Result and .Wait() — SYNCHRONOUS blockers.
//    - task.Result blocks until the task completes, then returns the value.
//    - task.Wait() blocks until the task completes (no return value).
//    - BOTH are safe on background threads or when the task is already completed.
//    - BOTH are DANGEROUS on the UI thread (or any thread with a SynchronizationContext).
//
// 3. .GetAwaiter().GetResult() — same problem, slightly different exception behavior.
//
// 4. THE FIX: Always use "await" instead of .Result or .Wait().
//    - "await" yields the thread, so the UI thread is free to process the continuation.
//    - No deadlock!
//
// ANALOGY:
// --------
// Imagine you're in a narrow hallway (the UI thread). You tell someone to go
// fetch something and bring it back through the hallway. Then you stand in the
// hallway and refuse to move until they return (.Result). But they can't get
// back because YOU are blocking the hallway. Nobody moves. Deadlock!
//
// "await" = you step aside and let them walk back through.
// ============================================================================

public partial class Step14ViewModel : StepViewModelBase
{
    private readonly Dispatcher _dispatcher;

    public Step14ViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    // ========================================================================
    // DEMO 1: Show the CORRECT way (await) working fine.
    // ========================================================================
    [RelayCommand]
    private async Task CorrectWay()
    {
        Log("--- The CORRECT Way: Using await ---\n");

        Log("   ? Calling async method with await...");
        string result = await GetDataAsync();
        Log($"   ? Got result: {result}");
        Log("   ?? No deadlock! The UI thread was free during the await.\n");
    }

    // ========================================================================
    // DEMO 2: Show .Result on a background thread (safe, but not recommended).
    // ========================================================================
    [RelayCommand]
    private async Task ResultOnBackgroundThread()
    {
        Log("--- .Result on a Background Thread (safe but not recommended) ---\n");

        Log("   ?? Running on background thread via Task.Run...");

        string result = await Task.Run(() =>
        {
            // This is a BACKGROUND thread — no SynchronizationContext.
            // .Result won't deadlock here (but await is still preferred).
            Task<string> task = GetDataAsync();
            return task.Result; // Safe here, dangerous on UI thread!
        });

        Log($"   ? Got result: {result}");
        Log("   ?? This worked because Task.Run uses a thread pool thread.");
        Log("   ?? The same code on the UI thread would DEADLOCK.\n");
    }

    // ========================================================================
    // DEMO 3: Demonstrate the DANGER — a simulated near-deadlock scenario.
    // We can't actually deadlock and recover, so we show the pattern and explain.
    // ========================================================================
    [RelayCommand]
    private void ShowDeadlockExplanation()
    {
        Log("--- Why .Result / .Wait() Causes Deadlocks ---\n");

        Log("   ?? The Dangerous Pattern:");
        Log("   ???????????????????????????????????????????????????");
        Log("   ?  // ON THE UI THREAD:                           ?");
        Log("   ?  string result = GetDataAsync().Result; // ??   ?");
        Log("   ???????????????????????????????????????????????????");
        Log("");
        Log("   ?? What happens step-by-step:");
        Log("   1?? GetDataAsync() starts and hits 'await Task.Delay(500)'.");
        Log("   2?? The await captures the SynchronizationContext (UI thread).");
        Log("   3?? .Result BLOCKS the UI thread, waiting for the task to finish.");
        Log("   4?? Task.Delay completes, and the continuation wants to run...");
        Log("   5?? ...but it needs the UI thread (captured in step 2).");
        Log("   6?? The UI thread is blocked by .Result (step 3).");
        Log("   7?? ?? DEADLOCK! Both are waiting for each other forever.");
        Log("");
        Log("   ? The Fix:");
        Log("   ???????????????????????????????????????????????????");
        Log("   ?  string result = await GetDataAsync(); // ?    ?");
        Log("   ???????????????????????????????????????????????????");
        Log("   await RELEASES the UI thread, so the continuation can run.\n");
    }

    // ========================================================================
    // DEMO 4: ConfigureAwait(false) as an escape hatch.
    // ========================================================================
    [RelayCommand]
    private async Task ConfigureAwaitEscape()
    {
        Log("--- ConfigureAwait(false) as an Escape Hatch ---\n");

        Log("   ?? If a library method uses ConfigureAwait(false) internally,");
        Log("      it does NOT need the UI thread to resume.");
        Log("      This PREVENTS the deadlock even with .Result.\n");

        // This works because GetDataWithConfigureAwaitAsync uses ConfigureAwait(false).
        string result = await Task.Run(() =>
        {
            // Even though we use .Result, the async method inside uses
            // ConfigureAwait(false), so it won't try to resume on UI thread.
            return GetDataWithConfigureAwaitAsync().Result;
        });

        Log($"   ? Got result: {result}");
        Log("   ?? ConfigureAwait(false) told the continuation:");
        Log("      'Don't bother returning to the UI thread.'");
        Log("      So there's no conflict, even with .Result on a thread pool thread.\n");
        Log("   ?? Best practice: ALWAYS prefer await over .Result/.Wait().");
        Log("      ConfigureAwait(false) is for LIBRARY code, not a deadlock fix.\n");
    }

    // --- Helper methods ---

    /// <summary>
    /// Standard async method — captures SynchronizationContext by default.
    /// Calling .Result on the UI thread with this method will DEADLOCK.
    /// </summary>
    private static async Task<string> GetDataAsync()
    {
        await Task.Delay(500);
        return "Data fetched successfully!";
    }

    /// <summary>
    /// Async method with ConfigureAwait(false) — does NOT capture the SynchronizationContext.
    /// Safer for library code, but the continuation might run on a different thread.
    /// </summary>
    private static async Task<string> GetDataWithConfigureAwaitAsync()
    {
        await Task.Delay(500).ConfigureAwait(false);
        return "Data fetched with ConfigureAwait(false)!";
    }
}
