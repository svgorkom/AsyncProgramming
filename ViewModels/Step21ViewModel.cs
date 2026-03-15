using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 21 VIEWMODEL: SynchronizationContext DEEP DIVE
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. SynchronizationContext — An abstraction that controls WHERE code runs
//    after an await. Different frameworks provide different contexts:
//    - WPF: DispatcherSynchronizationContext (posts to UI thread).
//    - WinForms: WindowsFormsSynchronizationContext (posts to UI thread).
//    - ASP.NET Core: NO SynchronizationContext (uses thread pool).
//    - Console apps: NO SynchronizationContext (uses thread pool).
//
// 2. HOW AWAIT USES IT:
//    - Before yielding, await captures SynchronizationContext.Current.
//    - When the awaited task completes, the continuation is posted BACK
//      to that captured context.
//    - In WPF: this means "resume on the UI thread."
//    - With no context: "resume on any available thread pool thread."
//
// 3. ConfigureAwait(false) — SKIPS capturing the context.
//    - The continuation runs on whatever thread is available.
//    - Useful in library code that doesn't need the UI thread.
//    - Small performance gain (avoids the Post overhead).
//
// 4. WHY THIS MATTERS:
//    - Understanding SynchronizationContext explains:
//      a) Why you CAN update UI after await (context posts back to UI thread).
//      b) Why .Result deadlocks (blocks UI thread + context tries to post there).
//      c) Why ConfigureAwait(false) prevents deadlocks.
//      d) Why ASP.NET Core doesn't have this problem (no context to capture).
//
// ANALOGY:
// --------
// SynchronizationContext = a postal address for your thread.
// When you send a letter (start async work), you write your return address.
// When the reply comes back, it's delivered to that address (your thread).
// ConfigureAwait(false) = "deliver the reply to whoever is available."
// ============================================================================

public partial class Step21ViewModel : StepViewModelBase
{
    private readonly Dispatcher _dispatcher;

    public Step21ViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    // ========================================================================
    // DEMO 1: Show the current SynchronizationContext.
    // ========================================================================
    [RelayCommand]
    private async Task ShowContext()
    {
        Log("--- Current SynchronizationContext ---\n");

        var ctx = SynchronizationContext.Current;
        Log($"   ?? SynchronizationContext.Current:");
        Log($"      Type: {ctx?.GetType().Name ?? "null (no context)"}");
        Log($"      Thread ID: {Environment.CurrentManagedThreadId}");
        Log("");

        // After await, we're back on the same thread (because context captured it).
        await Task.Delay(500);
        var ctxAfter = SynchronizationContext.Current;
        Log($"   ?? After await Task.Delay:");
        Log($"      Type: {ctxAfter?.GetType().Name ?? "null"}");
        Log($"      Thread ID: {Environment.CurrentManagedThreadId}");
        Log("      ? Same thread! Context brought us back.\n");

        // Inside Task.Run, there is NO SynchronizationContext.
        await Task.Run(() =>
        {
            var bgCtx = SynchronizationContext.Current;
            _dispatcher.Invoke(() =>
            {
                Log($"   ?? Inside Task.Run:");
                Log($"      Type: {bgCtx?.GetType().Name ?? "null (no context!)"}");
                Log($"      Thread ID: {Environment.CurrentManagedThreadId} (background)");
                Log("      ?? No context! Thread pool threads don't have one.\n");
            });
        });
    }

    // ========================================================================
    // DEMO 2: Context capture vs ConfigureAwait(false).
    // ========================================================================
    [RelayCommand]
    private async Task ContextCapture()
    {
        Log("--- Context Capture vs ConfigureAwait(false) ---\n");

        int uiThread = Environment.CurrentManagedThreadId;
        Log($"   ?? UI Thread ID: {uiThread}\n");

        // Default: context IS captured ? resumes on UI thread.
        Log("   1?? await Task.Delay(300) — context captured:");
        await Task.Delay(300);
        Log($"      Thread after: {Environment.CurrentManagedThreadId} " +
            $"(same as UI? {Environment.CurrentManagedThreadId == uiThread})\n");

        // ConfigureAwait(false): context NOT captured ? may resume on different thread.
        Log("   2?? await Task.Delay(300).ConfigureAwait(false) — no capture:");
        await Task.Delay(300).ConfigureAwait(false);

        int threadAfter = Environment.CurrentManagedThreadId;

        // We might be on a background thread now, so use Dispatcher.
        _dispatcher.Invoke(() =>
        {
            Log($"      Thread after: {threadAfter} " +
                $"(same as UI? {threadAfter == uiThread})");
            Log("      ?? With ConfigureAwait(false), thread may change!\n");
        });
    }

    // ========================================================================
    // DEMO 3: How context explains deadlocks.
    // ========================================================================
    [RelayCommand]
    private void ExplainDeadlock()
    {
        Log("--- How SynchronizationContext Causes Deadlocks ---\n");

        Log("   ?? The Deadlock Scenario (WPF UI Thread):");
        Log("");
        Log("   1. You call: var result = GetDataAsync().Result;");
        Log("   2. GetDataAsync starts and hits: await Task.Delay(500);");
        Log("   3. 'await' captures SynchronizationContext (= UI thread).");
        Log("   4. .Result blocks the UI thread, waiting for the task.");
        Log("   5. Task.Delay finishes. Continuation is posted to context...");
        Log("   6. Context says: 'Run on UI thread.' But UI thread is blocked!");
        Log("   7. ?? DEADLOCK — both sides wait forever.");
        Log("");
        Log("   ? Fix 1: Use await (frees the thread for the continuation).");
        Log("   ? Fix 2: Use ConfigureAwait(false) inside the called method.");
        Log("             ? continuation doesn't need the UI thread.");
        Log("   ? Fix 3: Don't use .Result or .Wait() on UI thread. Ever.");
        Log("");
        Log("   ?? Framework Comparison:");
        Log("   ????????????????????????????????????????????????????????????");
        Log("   ? Framework        ? SynchronizationContext ? Deadlock risk ?");
        Log("   ????????????????????????????????????????????????????????????");
        Log("   ? WPF / WinForms   ? Yes (UI thread)       ? HIGH          ?");
        Log("   ? ASP.NET (legacy) ? Yes (request thread)  ? HIGH          ?");
        Log("   ? ASP.NET Core     ? No                    ? LOW           ?");
        Log("   ? Console App      ? No                    ? LOW           ?");
        Log("   ????????????????????????????????????????????????????????????\n");
    }

    // ========================================================================
    // DEMO 4: Post to context manually.
    // ========================================================================
    [RelayCommand]
    private async Task ManualPost()
    {
        Log("--- Manual SynchronizationContext.Post ---\n");

        var context = SynchronizationContext.Current;

        if (context is null)
        {
            Log("   ? No SynchronizationContext available.\n");
            return;
        }

        Log($"   ?? Captured context: {context.GetType().Name}");
        Log("   ?? Starting background work...\n");

        await Task.Run(() =>
        {
            // We're on a background thread. Cannot update UI directly.
            // But we have the captured context — we can post to it!

            for (int i = 1; i <= 5; i++)
            {
                Thread.Sleep(400); // Simulate work on background thread.

                // Post back to the UI thread via the captured context.
                int iteration = i;
                context.Post(_ =>
                {
                    Log($"   ?? Posted from background thread: iteration {iteration}");
                }, null);
            }
        });

        // Small delay to let the last Post arrive.
        await Task.Delay(100);
        Log("\n   ? All posts delivered to UI thread via SynchronizationContext.\n");
    }
}
