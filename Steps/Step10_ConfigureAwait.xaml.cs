using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 10: ConfigureAwait, Task.Run, AND THREAD AWARENESS
// ============================================================================
//
// KEY CONCEPTS:
// -------------
//
// 1. SynchronizationContext (Don't panic — it's simpler than it sounds!)
//    - WPF has a special "context" that knows about the UI thread.
//    - When you "await" something, this context is captured.
//    - After the await, the context brings you BACK to the UI thread.
//    - This is why you can do: await Task.Delay(1000); MyLabel.Text = "Done!";
//      The label update works because you're back on the UI thread.
//
// 2. ConfigureAwait(false)
//    - Tells the await: "I DON'T need to come back to the original thread."
//    - After the await, code may run on ANY thread pool thread.
//    - ?? DO NOT use this if you need to update UI after the await!
//    - ? DO use this in library code, data access layers, or non-UI helper methods.
//    - Why? It avoids the overhead of switching back to the UI thread (slightly faster).
//
// 3. Task.Run(() => { ... })
//    - Explicitly runs code on a BACKGROUND THREAD (from the thread pool).
//    - Perfect for CPU-intensive work (calculations, image processing, etc.).
//    - ?? Inside Task.Run, you CANNOT touch UI controls directly!
//    - To update UI from inside Task.Run, use Dispatcher.Invoke().
//
// 4. When to use what:
//    ??????????????????????????????????????????????????????????????????
//    ? Scenario                ? Use                                  ?
//    ??????????????????????????????????????????????????????????????????
//    ? I/O (network, file)     ? await directly (no Task.Run needed)  ?
//    ? CPU-heavy work          ? await Task.Run(() => HeavyWork())    ?
//    ? Library (no UI)         ? .ConfigureAwait(false) on awaits     ?
//    ? After await, update UI  ? Default (don't use ConfigureAwait)   ?
//    ??????????????????????????????????????????????????????????????????
// ============================================================================

public partial class Step10_ConfigureAwait : Page
{
    public Step10_ConfigureAwait()
    {
        InitializeComponent();
    }

    // ========================================================================
    // DEMO 1: Show which thread you're on before and after await.
    // ========================================================================
    private async void ShowThreads_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Thread ID Demonstration ---\n");

        // Before any await — we're on the UI thread.
        Log($"   ?? BEFORE await: Thread ID = {Environment.CurrentManagedThreadId} (UI thread)");

        // After a normal await — we return to the UI thread (same ID).
        await Task.Delay(500);
        Log($"   ?? AFTER await Task.Delay: Thread ID = {Environment.CurrentManagedThreadId} (still UI thread!)");

        // Inside Task.Run — we're on a background thread (different ID).
        await Task.Run(() =>
        {
            // ?? This runs on a BACKGROUND thread. Cannot touch UI controls here!
            // (We can't call Log() directly because it updates a TextBox.)
            int bgThreadId = Environment.CurrentManagedThreadId;

            // Use Dispatcher.Invoke to safely send a message to the UI thread.
            Dispatcher.Invoke(() =>
            {
                Log($"   ?? INSIDE Task.Run: Thread ID = {bgThreadId} (background thread!)");
            });
        });

        // After awaiting Task.Run — back on the UI thread.
        Log($"   ?? AFTER await Task.Run: Thread ID = {Environment.CurrentManagedThreadId} (back on UI thread!)");

        // With ConfigureAwait(false) — might NOT be the UI thread.
        // (In practice, in a WPF app, this is mainly useful in library code.)
        int threadBeforeConfigureAwait = Environment.CurrentManagedThreadId;
        await Task.Delay(500).ConfigureAwait(false);
        int threadAfterConfigureAwait = Environment.CurrentManagedThreadId;

        // We might be on a different thread now, so use Dispatcher to safely update UI.
        Dispatcher.Invoke(() =>
        {
            Log($"\n   ?? BEFORE ConfigureAwait(false): Thread ID = {threadBeforeConfigureAwait}");
            Log($"   ?? AFTER  ConfigureAwait(false): Thread ID = {threadAfterConfigureAwait}");
            Log("   ?? With ConfigureAwait(false), the thread MAY change after await.\n");
        });
    }

    // ========================================================================
    // DEMO 2: Using Task.Run for CPU-heavy work.
    // ========================================================================
    private async void TaskRun_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Task.Run for CPU-Heavy Work ---\n");

        Log("   ? Starting CPU-heavy calculation on background thread...");
        Log($"   ?? UI thread ID: {Environment.CurrentManagedThreadId}");

        // Task.Run moves the heavy work to a background thread.
        // The UI stays responsive while the calculation runs!
        long result = await Task.Run(() =>
        {
            // This runs on a background thread — the UI is NOT blocked.
            // Simulate heavy CPU work (counting to 100 million).
            long sum = 0;
            for (int i = 0; i < 100_000_000; i++)
            {
                sum += i;
            }
            return sum;
        });

        // After await Task.Run, we're back on the UI thread — safe to update controls.
        Log($"   ? Calculation result: {result:N0}");
        Log($"   ?? Back on UI thread: {Environment.CurrentManagedThreadId}");
        Log("   ?? The UI stayed responsive during the calculation!\n");
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
